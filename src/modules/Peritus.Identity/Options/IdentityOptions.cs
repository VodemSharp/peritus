using Peritus.Types.Jwt;

namespace Peritus.Identity.Options;

public class IdentityOptions
{
    public JwtKey JwtKey { get; set; }
    public JwtIssuer JwtIssuer { get; set; }
    public JwtAudience JwtAudience { get; set; }

    public TimeSpan AccessTokenExpiry { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan RefreshTokenExpiry { get; set; } = TimeSpan.FromDays(1);
    public TimeSpan DefaultLockoutTimeSpan { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan EmailConfirmationTokenExpiry { get; set; } = TimeSpan.FromHours(24);
    public TimeSpan PasswordResetTokenExpiry { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan PhoneNumberVerificationTokenExpiry { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan TwoFactorTokenExpiry { get; set; } = TimeSpan.FromMinutes(5);

    public bool RequireConfirmedEmail { get; set; }
    public int MaxFailedAccessAttempts { get; set; } = 5;

    public required string GoogleClientId { get; set; }
}
