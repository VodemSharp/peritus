using Microsoft.EntityFrameworkCore;
using Peritus.FluentResults;
using Peritus.Identity.Persistence;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.Features.Profile;

public class ProfileGetFeature(IdentityDbContext db)
{
    public async Task<FluentResult<Result>> ExecuteAsync(Context context, CancellationToken ct)
    {
        var result = await db.Users
            .Where(x => x.Id == context.UserId)
            .Select(x => new Result
            {
                Email = x.Email
            })
            .SingleAsync(ct);

        return FluentResult<Result>.Success(result);
    }

    public class Context
    {
        public required UserId UserId { get; set; }
    }

    public class Result
    {
        public required Email Email { get; set; }
    }
}
