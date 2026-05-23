using Microsoft.EntityFrameworkCore;
using Peritus.FluentResults;
using Peritus.Identity.Persistence;
using Peritus.Identity.Types;
using Peritus.Types.Identity.Users;
using Peritus.Types.Tokens;

namespace Peritus.Identity.Features.Accounts;

public class SessionListFeature(IdentityDbContext db)
{
    public async Task<FluentResult<List<Result>>> ExecuteAsync(Context context, CancellationToken ct)
    {
        var sessions = await db.UserSessions
            .Where(x => x.UserId == context.UserId && x.Status == UserSessionStatus.Confirmed)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new Result
            {
                Id = x.Id,
                IpAddress = x.IpAddress,
                UserAgent = x.UserAgent,
                ExpiredAt = x.ExpiredAt,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(ct);

        return FluentResult<List<Result>>.Success(sessions);
    }

    public class Context
    {
        public required UserId UserId { get; set; }
    }

    public class Result
    {
        public required UserSessionId Id { get; set; }
        public required IpAddress? IpAddress { get; set; }
        public required UserAgent UserAgent { get; set; }
        public required DateTime ExpiredAt { get; set; }
        public required DateTime CreatedAt { get; set; }
    }
}
