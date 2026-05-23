using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Peritus.FluentResults;
using Peritus.Identity.Persistence;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.Features.Accounts;

public partial class SessionRevokeFeature(
    IdentityDbContext db,
    ISessionValidator sessionValidator,
    ILogger<SessionRevokeFeature> logger)
{
    public async Task<FluentResult> ExecuteAsync(Context context, CancellationToken ct)
    {
        var session = await db.UserSessions
            .Where(x => x.Id == context.SessionId && x.UserId == context.UserId)
            .SingleOrDefaultAsync(ct);

        if (session is null)
        {
            LogSessionNotFound(context.UserId.Value, context.SessionId.Value);
            return FluentResult.NotFound("Session not found.");
        }

        session.Status = UserSessionStatus.Terminated;
        db.UserSessions.Update(session);
        await db.SaveChangesAsync(ct);

        // Invalidate cache so the token is rejected immediately
        await sessionValidator.RemoveAsync(session.AccessTokenId, ct);

        return FluentResult.Success();
    }

    public class Context
    {
        public required UserId UserId { get; set; }
        public required UserSessionId SessionId { get; set; }
    }

    [LoggerMessage(LogLevel.Warning, "Session not found for user {UserId}: {SessionId}")]
    private partial void LogSessionNotFound(Guid userId, Guid sessionId);
}
