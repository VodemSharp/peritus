using Microsoft.EntityFrameworkCore;
using Peritus.FluentResults;
using Peritus.Identity.Helpers;
using Peritus.Identity.Persistence;
using Peritus.Identity.Persistence.Entities.Users;
using Peritus.Identity.Services.Abstractions;
using Peritus.Persistence.Extensions;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.Features.Accounts;

public class TwoFactorGenerateRecoveryCodesFeature(
    IdentityDbContext db,
    IUserService userService)
{
    public async Task<FluentResult<Result>> ExecuteAsync(Context context, CancellationToken ct)
    {
        var user = await userService.GetByIdAsync(context.UserId, ct);

        if (!user.TwoFactorEnabled)
        {
            return FluentResult<Result>.ValidationMessage("Two-factor authentication is not enabled.");
        }

        return await db.ExecuteInTransactionAsync(async () =>
        {
            await db.UserRecoveryCodes
                .Where(r => r.UserId == user.Id)
                .ExecuteDeleteAsync(ct);

            var rawCodes = TotpHelper.GenerateRecoveryCodes();

            await db.UserRecoveryCodes.AddRangeAsync(
                rawCodes.Select(rawCode => new UserRecoveryCode
                {
                    UserId = user.Id,
                    CodeHash = TokenHasher.HashToken(rawCode)
                }), ct);

            await db.SaveChangesAsync(ct);

            return FluentResult<Result>.Success(new Result
            {
                RecoveryCodes = rawCodes
            });
        }, ct);
    }

    public class Context
    {
        public required UserId UserId { get; set; }
    }

    public class Result
    {
        public required string[] RecoveryCodes { get; set; }
    }
}
