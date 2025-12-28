using Microsoft.EntityFrameworkCore;
using Peritus.Identity.Persistence.Entities.Roles;
using Peritus.Identity.Persistence.Entities.Users;
using Peritus.Identity.Types;
using Peritus.Persistence.Extensions;
using Peritus.Types.Identity.Roles;
using Peritus.Types.Identity.Users;
using Peritus.Types.Localization;
using Peritus.Types.Tokens;

namespace Peritus.Identity.Persistence;

public class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        #region Roles

        configurationBuilder.ConfigureGuidValue<RoleId>();
        configurationBuilder.ConfigureStringValue<RoleName>();

        #endregion

        #region Users

        configurationBuilder.ConfigureGuidValue<UserId>();
        configurationBuilder.ConfigureStringValue<Email>();
        configurationBuilder.ConfigureStringValue<Password>();
        configurationBuilder.ConfigureStringValue<CultureCode>();


        configurationBuilder.ConfigureGuidValue<UserSessionId>();
        configurationBuilder.ConfigureGuidValue<AccessTokenId>();
        configurationBuilder.ConfigureStringValue<IpAddress>();
        configurationBuilder.ConfigureStringValue<UserAgent>();
        configurationBuilder.ConfigureStringValue<UserSessionStatus>();
        configurationBuilder.ConfigureStringValue<UserSessionProvider>();
        configurationBuilder.ConfigureStringValue<RefreshToken>();

        #endregion
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("identity");

        #region Roles

        builder.ApplyConfiguration(new RoleConfiguration());
        builder.ApplyConfiguration(new UserRoleConfiguration());

        #endregion

        #region Users

        builder.ApplyConfiguration(new UserConfiguration());
        builder.ApplyConfiguration(new UserSessionConfiguration());

        #endregion
    }

    #region Roles

    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }

    #endregion

    #region Users

    public DbSet<User> Users { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }

    #endregion
}
