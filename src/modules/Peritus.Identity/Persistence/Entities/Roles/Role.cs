using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Peritus.Persistence.Extensions;
using Peritus.Types.Identity.Roles;

namespace Peritus.Identity.Persistence.Entities.Roles;

public class Role
{
    public RoleId Id { get; init; }
    public RoleName Name { get; init; }

    public ICollection<UserRole>? UserRoles { get; set; }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasGuidValueGenerator();

        builder.Property(x => x.Name).HasMaxLength(16);
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
