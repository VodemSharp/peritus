using Microsoft.Extensions.Logging;
using Peritus.FluentResults;
using Peritus.Identity.Persistence;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Types.Tokens;

namespace Peritus.Identity.Features.Accounts;

public partial class SignOutFeature(
    ILogger<SignOutFeature> logger,
    ISessionValidator sessionValidator,
    IUserSessionService userSessionService,
    IdentityDbContext db)
{
    public async Task<FluentResult> ExecuteAsync(Context context, CancellationToken ct)
    {
        var userSession = await userSessionService.FindByAccessTokenIdAsync(context.AccessTokenId, ct);

        if (userSession != null)
        {
            userSession.Status = UserSessionStatus.Terminated;
            db.UserSessions.Update(userSession);
            await db.SaveChangesAsync(ct);
        }
        else
        {
            LogUserSessionWasNotFound(context.AccessTokenId.Value);
        }

        await sessionValidator.RemoveAsync(context.AccessTokenId, ct);

        return FluentResult.Success();
    }

    [LoggerMessage(LogLevel.Warning, "User session was not found: {AccessTokenId}")]
    private partial void LogUserSessionWasNotFound(Guid accessTokenId);

    public class Context
    {
        public required AccessTokenId AccessTokenId { get; set; }
    }
}
