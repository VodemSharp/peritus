using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Peritus.Identity.Helpers;
using Peritus.Identity.Persistence;
using Peritus.Identity.Persistence.Entities.Roles;
using Peritus.Identity.Persistence.Entities.Users;
using Peritus.Identity.Services.Abstractions;
using Peritus.Types.Identity.Roles;
using Peritus.Types.Identity.Users;
using Peritus.Types.Localization;

namespace Peritus.Identity.Services;

public class UserService(IdentityDbContext db, IPasswordHasher<User> passwordHasher) : IUserService
{
    public async Task UpdateAsync(User user)
    {
        db.Users.Update(user);
        await db.SaveChangesAsync();
    }

    public async Task<bool> AnyAsync(Email email)
    {
        var normalizedEmail = EmailHelper.Normalize(email);
        return await db.Users.AnyAsync(x => x.NormalizedEmail == normalizedEmail);
    }

    public async Task<User?> GetOrDefaultAsync(Email email)
    {
        var normalizedEmail = EmailHelper.Normalize(email);
        return await db.Users.SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail);
    }

    public async Task<UserId> CreateAsync(
        Email email, Password password, List<RoleName>? roleNames = null, Culture? culture = null)
    {
        var roleIds = new List<RoleId>();
        culture ??= Culture.Default;
        roleNames ??= [];

        if (roleNames.Count > 0)
        {
            roleIds = await db.Roles
                .Where(x => roleNames.Contains(x.Name))
                .Select(x => x.Id)
                .ToListAsync();
        }

        var user = new User
        {
            Email = email,
            Culture = culture.Value.Code,
            UserRoles = roleIds.Select(x =>
                new UserRole
                {
                    RoleId = x
                }
            ).ToList()
        };

        user.PasswordHash = passwordHasher.HashPassword(user, password);

        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        return new UserId(user.Id);
    }
}
