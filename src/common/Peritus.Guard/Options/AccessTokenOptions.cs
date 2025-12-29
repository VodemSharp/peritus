using Peritus.Types.Jwt;

namespace Peritus.Guard.Options;

public class AccessTokenOptions
{
    public JwtKey Key { get; set; }
    public JwtIssuer Issuer { get; set; }
    public JwtAudience Audience { get; set; }
    public int ExpirySeconds { get; set; }

    public TimeSpan Expiry => TimeSpan.FromSeconds(ExpirySeconds);
}
