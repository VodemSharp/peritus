using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Peritus.Migrator.Options;
using Peritus.Identity.Persistence;
using Peritus.Identity.Persistence.Entities.Roles;
using Peritus.Identity.Persistence.Entities.Users;
using Peritus.Types.Identity.Roles;
using Peritus.Types.Localization;

namespace Peritus.Migrator;

public class Worker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime hostApplicationLifetime
) : BackgroundService
{
    public const string ActivitySourceName = "Migrations";
    private static readonly ActivitySource _activitySource = new(ActivitySourceName);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        const string activityName = "Migrating Database";
        using var activity = _activitySource.StartActivity(activityName, ActivityKind.Client);

        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            await using var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var adminOptions = scope.ServiceProvider.GetRequiredService<IOptions<AdminOptions>>().Value;
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
            var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

            await RunMigrationAsync(db, ct);
            await SeedDataAsync(db, timeProvider, passwordHasher, adminOptions, ct);
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            throw;
        }

        hostApplicationLifetime.StopApplication();
    }

    private static async Task RunMigrationAsync(IdentityDbContext db, CancellationToken ct)
    {
        await db.Database.MigrateAsync(ct);
    }

    private static async Task SeedDataAsync(
        IdentityDbContext db,
        TimeProvider timeProvider,
        IPasswordHasher<User> passwordHasher,
        AdminOptions adminOptions,
        CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

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
            await db.Set<Role>().AddRangeAsync(newRoles);
            await db.SaveChangesAsync(ct);
        }

        // Users
        var adminRoleNames = new List<RoleName>
        {
            RoleName.Admin
        };

        var anyAdmin = await db.Set<User>().AnyAsync(x => x.Email == adminOptions.Email, ct);
        var adminRoles = await db.Set<Role>().Where(x => adminRoleNames.Contains(x.Name)).ToListAsync(ct);

        // ReSharper disable once InvertIf
        if (!anyAdmin)
        {
            var user = new User
            {
                LastLoginAt = now,
                Email = adminOptions.Email,
                Culture = Culture.Default.Code,
                UserRoles = adminRoles.Select(role =>
                    new UserRole
                    {
                        RoleId = role.Id
                    }
                ).ToList()
            };

            user.PasswordHash = passwordHasher.HashPassword(user, adminOptions.Password);

            await db.Set<User>().AddAsync(user, ct);
            await db.SaveChangesAsync(ct);
        }
    }
}
