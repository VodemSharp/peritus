using Microsoft.EntityFrameworkCore;
using Peritus.FluentResults;
using Peritus.Guard.Extensions;
using Peritus.Identity.Persistence;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Types.Identity.Users;
using Peritus.Types.Tokens;

namespace Peritus.Identity.Features.Auth;

public class TokenRefreshFeature(
    IdentityDbContext db,
    ITokenService tokenService,
    ISessionValidator sessionValidator,
    TimeProvider timeProvider)
{
    public async Task<FluentResult<Result>> ExecuteAsync(Context context, CancellationToken ct)
    {
        var tokenResult = await tokenService.GetPrincipalFromExpiredTokenAsync(context.AccessToken);
        if (!tokenResult.IsSuccess)
        {
            return FluentResult<Result>.ValidationProblem(nameof(context.AccessToken), "Invalid access token.");
        }

        var principal = tokenResult.Result;
        var userId = principal.GetUserId();
        var accessTokenId = principal.GetAccessTokenId();

        var userSession = await db.UserSessions
            .Include(x => x.User)
            .ThenInclude(x => x!.UserRoles)!
            .ThenInclude(userRoles => userRoles.Role!)
            .SingleOrDefaultAsync(t => t.UserId == userId && t.RefreshToken == context.RefreshToken, ct);

        if (userSession == null)
        {
            return FluentResult<Result>.ValidationProblem(nameof(context.RefreshToken), "Refresh token not found.");
        }

        if (userSession.Status == UserSessionStatus.Terminated)
        {
            return FluentResult<Result>.ValidationProblem(nameof(context.RefreshToken),
                "User session has been terminated.");
        }

        if (userSession.ExpiredAt < timeProvider.GetUtcNow().UtcDateTime)
        {
            return FluentResult<Result>.ValidationProblem(nameof(context.RefreshToken),
                "User session has been expired.");
        }

        var newAccessTokenId = AccessTokenId.Create();
        var newAuthTokens = await tokenService.GenerateTokensAsync(userId, newAccessTokenId, ct);

        userSession.AccessTokenId = newAccessTokenId;
        userSession.RefreshToken = newAuthTokens.RefreshToken;
        userSession.IpAddress = context.IpAddress;
        userSession.UserAgent = context.UserAgent;

        db.UserSessions.Update(userSession);
        await db.SaveChangesAsync(ct);

        // Swap cache keys: old token is no longer valid, new one is
        await sessionValidator.RemoveAsync(accessTokenId, ct);
        await sessionValidator.SetAsync(newAccessTokenId, userSession.ExpiredAt, ct);

        return FluentResult<Result>.Success(
            new Result
            {
                AccessToken = newAuthTokens.AccessToken,
                RefreshToken = newAuthTokens.RefreshToken
            }
        );
    }

    public class Context
    {
        public required AccessToken AccessToken { get; set; }
        public required RefreshToken RefreshToken { get; set; }
        public required IpAddress? IpAddress { get; set; }
        public required UserAgent UserAgent { get; set; }
    }

    public class Result
    {
        public required AccessToken AccessToken { get; set; }
        public required RefreshToken RefreshToken { get; set; }
    }
}
