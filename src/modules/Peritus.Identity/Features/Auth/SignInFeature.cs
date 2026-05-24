using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Peritus.Cache.Distributed;
using Peritus.FluentResults;
using Peritus.Identity.Persistence;
using Peritus.Identity.Persistence.Entities.Users;
using Peritus.Identity.Services.Abstractions;
using Peritus.Persistence.Extensions;
using Peritus.Types.Identity.Users;
using Peritus.Types.Tokens;

namespace Peritus.Identity.Features.Auth;

public partial class SignInFeature(
    IPasswordHasher<User> passwordHasher,
    IUserSessionService userSessionService,
    IUserService userService,
    IDistributedCacheService cache,
    IOptions<IdentityOptions> identityOptions,
    TimeProvider timeProvider,
    ITokenService tokenService,
    ISessionValidator sessionValidator,
    IdentityDbContext db,
    ILogger<SignInFeature> logger)
{
    private readonly IdentityOptions _options = identityOptions.Value;

    public async Task<FluentResult<Result>> ExecuteAsync(Context context, CancellationToken ct)
    {
        var user = await userService.FindByEmailAsync(context.Email, ct);

        if (user is null)
        {
            return FluentResult<Result>.ValidationProblem(nameof(context.Password), "Invalid credentials.");
        }

        var remainingLockout = await GetRemainingLockoutAsync(user, ct);
        if (remainingLockout.HasValue)
        {
            return FluentResult<Result>.ValidationProblem(nameof(context.Password),
                $"Account locked due to multiple failed attempts. Try again in {remainingLockout.Value.TotalMinutes:F0} minutes.");
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, context.Password);

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, context.Password);
            await userService.UpdateAsync(user, ct);
        }

        switch (result)
        {
            case PasswordVerificationResult.Failed:
                await RecordAccessFailedAsync(user, ct);
                return FluentResult<Result>.ValidationProblem(nameof(context.Password), "Invalid credentials.");

            case PasswordVerificationResult.SuccessRehashNeeded:
            case PasswordVerificationResult.Success:
                await ResetAccessFailedAsync(user, ct);

                if (_options.RequireConfirmedEmail && !user.EmailConfirmed)
                {
                    return FluentResult<Result>.ValidationProblem(nameof(context.Email),
                        "Email not confirmed. Please check your inbox.");
                }

                if (!user.TwoFactorEnabled)
                {
                    return await db.ExecuteInTransactionAsync(async () =>
                    {
                        var sessionResult = await userSessionService.CreateAsync(
                            user.Id, context.IpAddress, context.UserAgent, ct: ct);

                        await sessionValidator.SetAsync(sessionResult.AccessTokenId, sessionResult.ExpiredAt, ct);

                        return FluentResult<Result>.Success(new Result
                        {
                            AccessToken = sessionResult.Tokens.AccessToken,
                            RefreshToken = sessionResult.Tokens.RefreshToken
                        });
                    }, ct);
                }

                var twoFactorToken = await tokenService.GenerateTwoFactorTokenAsync(user.Id, ct);
                return FluentResult<Result>.Success(new Result
                {
                    RequiresTwoFactor = true,
                    TwoFactorToken = twoFactorToken
                });

            default:
                throw new ApplicationException("Unexpected result");
        }
    }

    private async Task<TimeSpan?> GetRemainingLockoutAsync(User user, CancellationToken ct = default)
    {
        var lockoutKey = IdentityCacheKeys.AuthLockout(user.Id);
        var lockoutValue = await cache.GetStringAsync(lockoutKey, ct);

        if (string.IsNullOrEmpty(lockoutValue))
        {
            return null;
        }

        if (!long.TryParse(lockoutValue, out var lockoutEndMs))
        {
            return null;
        }

        var lockoutEnd = DateTimeOffset.FromUnixTimeMilliseconds(lockoutEndMs);
        var remaining = lockoutEnd - timeProvider.GetUtcNow();
        if (remaining > TimeSpan.Zero)
        {
            return remaining;
        }

        await cache.RemoveAsync(lockoutKey, ct);
        return null;
    }

    private async Task RecordAccessFailedAsync(User user, CancellationToken ct = default)
    {
        var failedKey = IdentityCacheKeys.AuthFailed(user.Id);
        var lockoutKey = IdentityCacheKeys.AuthLockout(user.Id);

        var remainingLockout = await GetRemainingLockoutAsync(user, ct);
        if (remainingLockout.HasValue)
        {
            return;
        }

        var countValue = await cache.GetStringAsync(failedKey, ct);
        var count = int.TryParse(countValue, out var parsed) ? parsed : 0;
        count++;

        await cache.SetStringAsync(
            failedKey,
            count.ToString(),
            _options.DefaultLockoutTimeSpan,
            ct);

        if (count < _options.MaxFailedAccessAttempts)
        {
            return;
        }

        var lockoutEnd = timeProvider.GetUtcNow().Add(_options.DefaultLockoutTimeSpan);
        var lockoutEndMs = lockoutEnd.ToUnixTimeMilliseconds();

        await cache.SetStringAsync(
            lockoutKey,
            lockoutEndMs.ToString(),
            _options.DefaultLockoutTimeSpan.Add(TimeSpan.FromMinutes(1)),
            ct);

        await cache.RemoveAsync(failedKey, ct);

        LogUserLockedOut(user.Id, lockoutEnd, count);
    }

    private async Task ResetAccessFailedAsync(User user, CancellationToken ct = default)
    {
        await cache.RemoveAsync(IdentityCacheKeys.AuthFailed(user.Id), ct);
        await cache.RemoveAsync(IdentityCacheKeys.AuthLockout(user.Id), ct);
    }

    [LoggerMessage(LogLevel.Warning, "User {UserId} locked out until {LockoutEnd} after {Count} failed attempts")]
    private partial void LogUserLockedOut(UserId userId, DateTimeOffset lockoutEnd, int count);

    public class Context
    {
        public required Email Email { get; set; }
        public required Password Password { get; set; }
        public required IpAddress? IpAddress { get; set; }
        public required UserAgent UserAgent { get; set; }
    }

    public class Result
    {
        public AccessToken? AccessToken { get; set; }
        public RefreshToken? RefreshToken { get; set; }
        public bool RequiresTwoFactor { get; set; }
        public string? TwoFactorToken { get; set; }
    }
}
