using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Peritus.AspNetCore.Extensions;
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

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/signin", async (
                Request request,
                SignInFeature feature,
                HttpContext httpContext,
                CancellationToken ct) =>
            {
                request.IpAddress = httpContext.GetRemoteIpAddress();
                request.UserAgent = httpContext.Request.GetUserAgent();

                var result = await feature.ExecuteAsync(request, ct);
                return result.ToResult();
            })
            .Produces<Response>()
            .WithTags("Auth")
            .WithSummary("Sign in");
    }

    private async Task<FluentResult<Response>> ExecuteAsync(Request request, CancellationToken ct)
    {
        var user = await userService.FindByEmailAsync(request.Email, ct);

        if (user is null)
        {
            return FluentResult<Response>.ValidationProblem(nameof(request.Password), "Invalid credentials.");
        }

        var remainingLockout = await GetRemainingLockoutAsync(user, ct);
        if (remainingLockout.HasValue)
        {
            return FluentResult<Response>.ValidationProblem(nameof(request.Password),
                $"Account locked due to multiple failed attempts. " +
                $"Try again in {remainingLockout.Value.TotalMinutes:F0} minutes.");
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            await userService.UpdateAsync(user, ct);
        }

        switch (result)
        {
            case PasswordVerificationResult.Failed:
                await RecordAccessFailedAsync(user, ct);
                return FluentResult<Response>.ValidationProblem(nameof(request.Password), "Invalid credentials.");

            case PasswordVerificationResult.SuccessRehashNeeded:
            case PasswordVerificationResult.Success:
                await ResetAccessFailedAsync(user, ct);

                if (_options.RequireConfirmedEmail && !user.EmailConfirmed)
                {
                    return FluentResult<Response>.ValidationProblem(nameof(request.Email),
                        "Email not confirmed. Please check your inbox.");
                }

                if (!user.TwoFactorEnabled)
                {
                    return await db.ExecuteInTransactionAsync(async () =>
                    {
                        var sessionResult = await userSessionService.CreateAsync(
                            user.Id, request.IpAddress, request.UserAgent, ct: ct);

                        await sessionValidator.SetAsync(sessionResult.AccessTokenId, sessionResult.ExpiredAt, ct);

                        return FluentResult<Response>.Success(new Response
                        {
                            AccessToken = sessionResult.Tokens.AccessToken,
                            RefreshToken = sessionResult.Tokens.RefreshToken
                        });
                    }, ct);
                }

                var twoFactorToken = await tokenService.GenerateTwoFactorTokenAsync(user.Id, ct);
                return FluentResult<Response>.Success(new Response
                {
                    RequiresTwoFactor = true,
                    TwoFactorToken = twoFactorToken
                });

            default:
                return FluentResult<Response>.InternalError("Unexpected sign-in result.");
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

    public class Request
    {
        [JsonPropertyName("email")] public required Email Email { get; set; }
        [JsonPropertyName("password")] public required Password Password { get; set; }
        [JsonIgnore] public IpAddress? IpAddress { get; set; }
        [JsonIgnore] public UserAgent UserAgent { get; set; }
    }

    public class Response
    {
        [JsonPropertyName("accessToken")] public AccessToken? AccessToken { get; set; }
        [JsonPropertyName("refreshToken")] public RefreshToken? RefreshToken { get; set; }

        [JsonPropertyName("requiresTwoFactor")]
        public bool RequiresTwoFactor { get; set; }

        [JsonPropertyName("twoFactorToken")] public string? TwoFactorToken { get; set; }
    }
}
