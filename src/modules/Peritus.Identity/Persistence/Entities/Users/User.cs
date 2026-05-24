using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Peritus.Identity.Persistence.Entities.Roles;
using Peritus.Persistence.Entities;
using Peritus.Persistence.Extensions;
using Peritus.Types.Identity.Users;
using Peritus.Types.Localization;

namespace Peritus.Identity.Persistence.Entities.Users;

public class User : TimestampedEntity
{
    public UserId Id { get; init; }

    public Email Email { get; set; }
    public Email NormalizedEmail { get; init; }

    public bool EmailConfirmed { get; set; }
    public PhoneNumber? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public string? TwoFactorSecret { get; set; }

    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string PasswordHash { get; set; } = null!;

    public CultureCode Culture { get; set; }

    public ICollection<UserRole>? UserRoles { get; init; }
    public ICollection<UserSession>? UserSessions { get; init; }
    public ICollection<UserRecoveryCode>? UserRecoveryCodes { get; init; }
    public ICollection<UserExternalLogin>? UserExternalLogins { get; init; }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasGuidValueGenerator();

        builder.HasIndex(x => x.CreatedAt);

        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();

        builder.Property(e => e.NormalizedEmail)
            .HasComputedColumnSql("LOWER(\"email\")", true)
            .HasMaxLength(256)
            .ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(e => e.NormalizedEmail).IsUnique();

        builder.Property(x => x.PhoneNumber).HasMaxLength(20);
        builder.Property(x => x.TwoFactorSecret).HasMaxLength(64);
        builder.Property(x => x.Culture).HasMaxLength(5).IsRequired();
    }
}
