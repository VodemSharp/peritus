using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Peritus.FluentResults;
using Peritus.Identity.Persistence;
using Peritus.Identity.Persistence.Entities.Users;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Persistence.Extensions;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.Features.Auth;

public class PasswordResetFeature(
    IUserService userService,
    IUserTokenService userTokenService,
    IPasswordHasher<User> passwordHasher,
    TimeProvider timeProvider,
    IdentityDbContext db)
{
    public async Task<FluentResult> ExecuteAsync(Context context, CancellationToken ct)
    {
        var user = await userService.FindByEmailAsync(context.Email, ct);

        if (user is null)
        {
            return FluentResult.ValidationProblem(nameof(context.Token), "Invalid or expired token.");
        }

        var result = await userTokenService.RedeemAsync(user.Id, UserTokenType.PasswordReset, context.Token, ct);

        if (!result.IsSuccess)
        {
            return FluentResult.ValidationProblem(nameof(context.Token), "Invalid or expired token.");
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
        public required Email Email { get; set; }
        public required string Token { get; set; }
        public required Password NewPassword { get; set; }
    }
}
