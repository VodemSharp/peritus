using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Peritus.Identity.Persistence;
using Peritus.Identity.Persistence.Entities.Roles;
using Peritus.Identity.Persistence.Entities.Users;
using Peritus.Identity.Services.Abstractions;
using Peritus.Types.Identity.Roles;
using Peritus.Types.Identity.Users;
using Peritus.Types.Localization;

namespace Peritus.Identity.Services;

public class UserService(
    IdentityDbContext db,
    IPasswordHasher<User> passwordHasher) : IUserService
{
    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        db.Users.Update(user);
        await db.SaveChangesAsync(ct);
    }

    public async Task<UserId> CreateAsync(
        Email email,
        Password password,
        List<RoleName>? roleNames = null,
        Culture? culture = null,
        CancellationToken ct = default)
    {
        var roleIds = new List<RoleId>();
        culture ??= Culture.Default;
        roleNames ??= [];

        if (roleNames.Count > 0)
        {
            roleIds = await db.Roles
                .Where(x => roleNames.Contains(x.Name))
                .Select(x => x.Id)
                .ToListAsync(ct);
        }

        var user = new User
        {
            Email = email,
            Culture = culture.Value.Code,
            UserRoles = roleIds.Select(x => new UserRole
            {
                RoleId = x
            }).ToList()
        };

        user.PasswordHash = passwordHasher.HashPassword(user, password);
        await db.Users.AddAsync(user, ct);
        await db.SaveChangesAsync(ct);

        return user.Id;
    }

    public async Task<User> GetByIdAsync(UserId userId, CancellationToken ct = default)
    {
        return await db.Users.FindAsync([userId], ct)
               ?? throw new InvalidOperationException($"User not found: {userId}");
    }

    public async Task<User?> FindByEmailAsync(Email email, CancellationToken ct = default)
    {
        var normalizedEmail = email.Normalize();
        return await db.Users.SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, ct);
    }

    public async Task<bool> AnyByEmailAsync(Email email, CancellationToken ct = default)
    {
        var normalizedEmail = email.Normalize();
        return await db.Users.AnyAsync(x => x.NormalizedEmail == normalizedEmail, ct);
    }
}
