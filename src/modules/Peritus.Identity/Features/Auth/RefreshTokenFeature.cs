using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Peritus.FluentResults;
using Peritus.Guard.Extensions;
using Peritus.Guard.Options;
using Peritus.Guard.Services.Abstractions;
using Peritus.Identity.Persistence;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Types.Identity.Users;
using Peritus.Types.Tokens;

namespace Peritus.Identity.Features.Auth;

public class RefreshTokenFeature(
    IdentityDbContext db,
    ITokenService tokenService,
    ITokenRevoker tokenRevoker,
    TimeProvider timeProvider,
    IOptions<AccessTokenOptions> accessTokenOptions)
{
    private readonly AccessTokenOptions _accessTokenOptions = accessTokenOptions.Value;

    public async Task<FluentResult<Result>> ExecuteAsync(Context context, CancellationToken ct)
    {
        const string refreshTokenNotFound = "Refresh token not found.";
        const string userSessionHasBeenTerminated = "User session has been terminated.";
        const string userSessionHasBeenExpired = "User session has been expired.";

        var principal = await tokenService.GetPrincipalFromExpiredTokenAsync(context.AccessToken);

        var userId = principal.GetUserId();

        var userSession = await db.UserSessions
            .Include(x => x.User)
            .ThenInclude(x => x!.UserRoles)!
            .ThenInclude(userRoles => userRoles.Role!)
            .SingleOrDefaultAsync(t => t.UserId == userId && t.RefreshToken == context.RefreshToken, ct);

        if (userSession == null)
        {
            return FluentResult<Result>.ValidationProblem(nameof(context.RefreshToken), refreshTokenNotFound);
        }

        if (userSession.Status == UserSessionStatus.Terminated)
        {
            return FluentResult<Result>.ValidationProblem(nameof(context.RefreshToken), userSessionHasBeenTerminated);
        }

        if (userSession.ExpiredAt < timeProvider.GetUtcNow().UtcDateTime)
        {
            return FluentResult<Result>.ValidationProblem(nameof(context.RefreshToken), userSessionHasBeenExpired);
        }

        var accessTokenId = principal.GetAccessTokenId();

        await tokenRevoker.RevokeAsync(accessTokenId, _accessTokenOptions.Expiry, ct);

        var newAccessTokenId = AccessTokenId.Create();
        var newAuthTokens = await tokenService.GenerateTokensAsync(userId, newAccessTokenId, ct);

        userSession.AccessTokenId = newAccessTokenId;
        userSession.RefreshToken = newAuthTokens.RefreshToken;
        userSession.IpAddress = context.IpAddress;
        userSession.UserAgent = context.UserAgent;

        db.UserSessions.Update(userSession);
        await db.SaveChangesAsync(ct);

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
