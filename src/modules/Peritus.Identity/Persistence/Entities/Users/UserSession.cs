using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Peritus.Identity.Types;
using Peritus.Persistence.Entities;
using Peritus.Persistence.Extensions;
using Peritus.Types.Identity.Users;
using Peritus.Types.Tokens;

namespace Peritus.Identity.Persistence.Entities.Users;

public class UserSession : TimestampedEntity
{
    public UserSessionId Id { get; set; }

    public IpAddress? IpAddress { get; set; }
    public UserAgent UserAgent { get; set; }

    public UserSessionStatus Status { get; set; }

    public AccessTokenId AccessTokenId { get; set; }
    public RefreshToken RefreshToken { get; set; }

    public DateTime ExpiredAt { get; set; }

    public UserId UserId { get; set; }
    public User? User { get; set; }

    public UserExternalLoginId? ExternalLoginId { get; set; }
    public UserExternalLogin? ExternalLogin { get; set; }
}

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasGuidValueGenerator();

        builder.Property(x => x.IpAddress).HasMaxLength(45);
        builder.Property(x => x.UserAgent).HasMaxLength(512);
        builder.Property(x => x.Status).HasMaxLength(16);
        builder.Property(x => x.AccessTokenId).HasMaxLength(36);
        builder.Property(x => x.AccessTokenId).HasMaxLength(36);
        builder.Property(x => x.RefreshToken).HasMaxLength(44);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ExternalLoginId);

        builder.HasOne(x => x.ExternalLogin)
            .WithMany()
            .HasForeignKey(x => x.ExternalLoginId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
