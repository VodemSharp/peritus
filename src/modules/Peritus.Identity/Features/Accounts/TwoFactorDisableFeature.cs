using Microsoft.EntityFrameworkCore;
using Peritus.FluentResults;
using Peritus.Identity.Persistence;
using Peritus.Identity.Services.Abstractions;
using Peritus.Persistence.Extensions;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.Features.Accounts;

public class TwoFactorDisableFeature(IdentityDbContext db, IUserService userService)
{
    public async Task<FluentResult> ExecuteAsync(Context context, CancellationToken ct)
    {
        var user = await userService.GetByIdAsync(context.UserId, ct);
        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;

        await db.ExecuteInTransactionAsync(async () =>
        {
            db.Users.Update(user);
            await db.SaveChangesAsync(ct);

            await db.UserRecoveryCodes
                .Where(r => r.UserId == user.Id)
                .ExecuteDeleteAsync(ct);
        }, ct);

        return FluentResult.Success();
    }

    public class Context
    {
        public required UserId UserId { get; set; }
    }
}
