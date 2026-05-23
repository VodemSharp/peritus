using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Peritus.Identity.Types;
using Peritus.Persistence.Entities;
using Peritus.Persistence.Extensions;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.Persistence.Entities.Users;

public class UserExternalLogin : TimestampedEntity
{
    public UserExternalLoginId Id { get; set; }
    public UserId UserId { get; set; }
    public ExternalLoginProvider Provider { get; set; }
    public string ProviderKey { get; set; } = null!;
    public string? ProviderDisplayName { get; set; }
    public User User { get; set; } = null!;
}

public class UserExternalLoginConfiguration : IEntityTypeConfiguration<UserExternalLogin>
{
    public void Configure(EntityTypeBuilder<UserExternalLogin> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasGuidValueGenerator();

        builder.Property(x => x.Provider).HasMaxLength(32);
        builder.Property(x => x.ProviderKey).HasMaxLength(256);
        builder.Property(x => x.ProviderDisplayName).HasMaxLength(256);

        builder.HasIndex(x => new { x.Provider, x.ProviderKey }).IsUnique();
        builder.HasIndex(x => x.UserId);
    }
}
