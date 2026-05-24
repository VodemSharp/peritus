using Peritus.FluentResults;
using Peritus.Identity.Helpers;
using Peritus.Identity.Persistence;
using Peritus.Identity.Services.Abstractions;
using Peritus.Persistence.Extensions;
using Peritus.Types.Identity.Users;
using Peritus.Types.Tokens;

namespace Peritus.Identity.Features.Auth;

public class SignInTwoFactorFeature(
    ITokenService tokenService,
    IUserService userService,
    IUserSessionService userSessionService,
    ISessionValidator sessionValidator,
    IdentityDbContext db)
{
    public async Task<FluentResult<Result>> ExecuteAsync(Context context, CancellationToken ct)
    {
        var tokenResult = await tokenService.ValidateTwoFactorTokenAsync(context.TwoFactorToken, ct);
        if (!tokenResult.IsSuccess)
        {
            return FluentResult<Result>.ValidationProblem(nameof(context.TwoFactorToken), "Invalid two-factor token.");
        }

        var userId = tokenResult.Result;
        var user = await userService.GetByIdAsync(userId, ct);

        if (!user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecret))
        {
            return FluentResult<Result>.ValidationProblem(nameof(context.Code),
                "Two-factor authentication is not enabled.");
        }

        if (!TotpHelper.ValidateCode(user.TwoFactorSecret, context.Code))
        {
            return FluentResult<Result>.ValidationProblem(nameof(context.Code), "Invalid code.");
        }

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

    public class Context
    {
        public required string TwoFactorToken { get; set; }
        public required string Code { get; set; }
        public required IpAddress? IpAddress { get; set; }
        public required UserAgent UserAgent { get; set; }
    }

    public class Result
    {
        public required AccessToken AccessToken { get; set; }
        public required RefreshToken RefreshToken { get; set; }
    }
}
