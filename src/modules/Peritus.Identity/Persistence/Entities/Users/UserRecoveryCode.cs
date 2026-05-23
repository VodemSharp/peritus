using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Peritus.Identity.Types;
using Peritus.Persistence.Entities.Abstractions;
using Peritus.Persistence.Extensions;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.Persistence.Entities.Users;

public class UserRecoveryCode : ICreatedEntity
{
    public UserRecoveryCodeId Id { get; set; }
    public UserId UserId { get; set; }
    public string CodeHash { get; set; } = null!;
    public DateTime? RedeemedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public User User { get; set; } = null!;
}

public class UserRecoveryCodeConfiguration : IEntityTypeConfiguration<UserRecoveryCode>
{
    public void Configure(EntityTypeBuilder<UserRecoveryCode> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasGuidValueGenerator();

        builder.Property(x => x.CodeHash).HasMaxLength(64);

        builder.HasIndex(x => x.UserId);
    }
}
