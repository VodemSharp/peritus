using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Peritus.Identity.Persistence.Entities.Users;
using Peritus.Persistence.Entities;
using Peritus.Types.Identity.Roles;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.Persistence.Entities.Roles;

public class UserRole : CreatedEntity
{
    public UserId UserId { get; init; }
    public RoleId RoleId { get; init; }

    public User? User { get; set; }
    public Role? Role { get; set; }
}

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.HasKey(x => new
        {
            x.UserId,
            x.RoleId
        });

        builder.HasOne(x => x.User)
            .WithMany(x => x.UserRoles)
            .HasForeignKey(x => x.UserId);

        builder.HasOne(x => x.Role)
            .WithMany(x => x.UserRoles)
            .HasForeignKey(x => x.RoleId);

        builder.HasIndex(x => x.RoleId);
    }
}
