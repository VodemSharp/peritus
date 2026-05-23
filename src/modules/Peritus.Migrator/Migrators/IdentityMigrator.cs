using DbUp;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Peritus.Identity.Persistence;
using Peritus.Identity.Persistence.Entities.Roles;
using Peritus.Identity.Persistence.Entities.Users;
using Peritus.Migrator.Options;
using Peritus.Persistence.Extensions;
using Peritus.Types.Identity.Roles;
using Peritus.Types.Localization;

namespace Peritus.Migrator.Migrators;

public class IdentityMigrator(
    IConfiguration configuration,
    IdentityDbContext db,
    IPasswordHasher<User> passwordHasher,
    IOptions<IdentitySeedOptions> seedOptions)
{
    public void Migrate()
    {
        var connectionString = configuration.GetConnectionString("db")
                               ?? throw new InvalidOperationException("Connection string 'db' is not configured.");

        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(IdentityDbContext).Assembly)
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            throw new InvalidOperationException("Database migration failed.", result.Error);
        }
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await db.ExecuteInTransactionAsync(async () =>
        {
            var options = seedOptions.Value;

            // Roles
            var roles = new List<RoleName>
            {
                RoleName.Admin
            };

            var existedRoles = await db.Set<Role>().Where(x => roles.Contains(x.Name)).ToListAsync(ct);
            var newRoles = roles.Where(x => existedRoles.All(er => er.Name != x))
                .Select(x => new Role
                {
                    Name = x
                }).ToArray();

            if (newRoles.Length != 0)
            {
                await db.Set<Role>().AddRangeAsync(newRoles, ct);
                await db.SaveChangesAsync(ct);
            }

            // Users
            var adminRoleNames = new List<RoleName>
            {
                RoleName.Admin
            };

            var anyAdmin = await db.Set<User>().AnyAsync(x => x.Email == options.AdminEmail, ct);
            var adminRoles = await db.Set<Role>().Where(x => adminRoleNames.Contains(x.Name)).ToListAsync(ct);

            if (!anyAdmin)
            {
                var user = new User
                {
                    Email = options.AdminEmail,
                    Culture = Culture.Default.Code,
                    UserRoles = adminRoles.Select(role =>
                        new UserRole
                        {
                            RoleId = role.Id
                        }
                    ).ToList()
                };

                user.PasswordHash = passwordHasher.HashPassword(user, options.AdminPassword);

                await db.Set<User>().AddAsync(user, ct);
                await db.SaveChangesAsync(ct);
            }
        }, ct);
    }
}
