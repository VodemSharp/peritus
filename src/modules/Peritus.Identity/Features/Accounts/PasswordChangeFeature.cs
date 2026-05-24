using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Peritus.FluentResults;
using Peritus.Identity.Persistence;
using Peritus.Identity.Persistence.Entities.Users;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Persistence.Extensions;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.Features.Accounts;

public class PasswordChangeFeature(
    IUserService userService,
    IPasswordHasher<User> passwordHasher,
    TimeProvider timeProvider,
    IdentityDbContext db)
{
    public async Task<FluentResult> ExecuteAsync(Context context, CancellationToken ct)
    {
        var user = await userService.GetByIdAsync(context.UserId, ct);

        var verifyResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, context.CurrentPassword);

        if (verifyResult == PasswordVerificationResult.Failed)
        {
            return FluentResult.ValidationProblem(nameof(context.CurrentPassword), "Current password is incorrect.");
        }

        await db.ExecuteInTransactionAsync(async () =>
        {
            user.PasswordHash = passwordHasher.HashPassword(user, context.NewPassword);
            db.Users.Update(user);
            await db.SaveChangesAsync(ct);

            await db.UserSessions
                .Where(s => s.UserId == user.Id && s.Status == UserSessionStatus.Confirmed)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(s => s.Status, UserSessionStatus.Terminated)
                        .SetProperty(s => s.UpdatedAt, timeProvider.GetUtcNow().UtcDateTime),
                    ct);
        }, ct);

        return FluentResult.Success();
    }

    public class Context
    {
        public required UserId UserId { get; set; }
        public required string CurrentPassword { get; set; }
        public required string NewPassword { get; set; }
    }
}
