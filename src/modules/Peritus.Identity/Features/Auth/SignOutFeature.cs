using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Peritus.Guard.Extensions;
using Peritus.Guard.Options;
using Peritus.Guard.Services.Abstractions;
using Peritus.Identity.Persistence;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Types.Tokens;

namespace Peritus.Identity.Features.Auth;

public partial class SignOutFeature(
    IOptions<AccessTokenOptions> accessTokenOptions,
    ILogger<SignOutFeature> logger,
    ITokenService tokenService,
    ITokenRevoker tokenRevoker,
    IdentityDbContext db)
{
    private readonly AccessTokenOptions _accessTokenOptions = accessTokenOptions.Value;

    public async Task ExecuteAsync(Context context, CancellationToken ct)
    {
        var principal = await tokenService.GetPrincipalFromExpiredTokenAsync(context.AccessToken);
        var accessTokenId = principal.GetAccessTokenId();

        var userSession = await db.UserSessions.SingleOrDefaultAsync(us => us.AccessTokenId == accessTokenId, ct);

        if (userSession != null)
        {
            userSession.Status = UserSessionStatus.Terminated;

            db.UserSessions.Update(userSession);
            await db.SaveChangesAsync(ct);
        }
        else
        {
            LogUserSessionWasNotFound(logger, accessTokenId);
        }

        await tokenRevoker.RevokeAsync(accessTokenId, _accessTokenOptions.Expiry, ct);
    }

    [LoggerMessage(LogLevel.Error, "User session was not found: {accessTokenId}")]
    private static partial void LogUserSessionWasNotFound(ILogger<SignOutFeature> logger, Guid accessTokenId);

    public class Context
    {
        public required AccessToken AccessToken { get; set; }
    }
}
