using Microsoft.EntityFrameworkCore;
using Peritus.FluentResults;
using Peritus.Identity.Persistence;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.Features.Accounts;

public class SessionRevokeAllFeature(IdentityDbContext db, ISessionValidator sessionValidator)
{
    public async Task<FluentResult> ExecuteAsync(Context context, CancellationToken ct)
    {
        var sessions = await db.UserSessions
            .Where(x => x.UserId == context.UserId && x.Status == UserSessionStatus.Confirmed && x.Id != context.CurrentSessionId)
            .ToListAsync(ct);

        foreach (var session in sessions)
        {
            session.Status = UserSessionStatus.Terminated;
            db.UserSessions.Update(session);

            // Invalidate each session cache entry
            await sessionValidator.RemoveAsync(session.AccessTokenId, ct);
        }

        await db.SaveChangesAsync(ct);

        return FluentResult.Success();
    }

    public class Context
    {
        public required UserId UserId { get; set; }
        public required UserSessionId CurrentSessionId { get; set; }
    }
}
