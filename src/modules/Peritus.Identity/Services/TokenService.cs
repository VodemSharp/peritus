using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Peritus.Guard.Claims;
using Peritus.Guard.Options;
using Peritus.Identity.Persistence;
using Peritus.Identity.Services.Abstractions;
using Peritus.Types.Identity.Users;
using Peritus.Types.Tokens;

namespace Peritus.Identity.Services;

public class TokenService(
    TimeProvider timeProvider,
    IdentityDbContext db,
    IOptions<AccessTokenOptions> accessTokenOptions
) : ITokenService
{
    private readonly AccessTokenOptions _accessTokenOptions = accessTokenOptions.Value;

    public async Task<ClaimsPrincipal> GetPrincipalFromExpiredTokenAsync(AccessToken token)
    {
        const string invalidTokenMessage = "Invalid token";

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = _accessTokenOptions.Audience,
            ValidateIssuer = true,
            ValidIssuer = _accessTokenOptions.Issuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_accessTokenOptions.Key)),
            ValidateLifetime = false
        };

        var tokenHandler = new JsonWebTokenHandler();
        var result = await tokenHandler.ValidateTokenAsync(token, tokenValidationParameters);

        if (!result.IsValid)
        {
            throw new SecurityTokenException(invalidTokenMessage);
        }

        var jsonToken = tokenHandler.ReadJsonWebToken(token);
        return jsonToken.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase)
            ? new ClaimsPrincipal(result.ClaimsIdentity)
            : throw new SecurityTokenException(invalidTokenMessage);
    }

    public async Task<AuthTokenPair> GenerateTokensAsync(
        UserId userId, AccessTokenId accessTokenId, CancellationToken ct = default)
    {
        var userRoles = await db.UserRoles
            .Include(x => x.Role)
            .Where(x => x.UserId == userId)
            .ToListAsync(ct);

        var claims = userRoles
            .Select(x => new Claim(ClaimTypes.Role, x.Role!.Name))
            .ToList();

        return new AuthTokenPair
        {
            AccessToken = GenerateAccessToken(userId, accessTokenId, claims),
            RefreshToken = GenerateRefreshToken()
        };
    }

    private AccessToken GenerateAccessToken(UserId userId, AccessTokenId accessTokenId, List<Claim> claims)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_accessTokenOptions.Key));

        var jwtClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, accessTokenId.Value.ToString()),
            new(CustomClaimTypes.UserId, userId.Value.ToString())
        }.Union(claims);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(jwtClaims),
            Issuer = _accessTokenOptions.Issuer,
            Audience = _accessTokenOptions.Audience,
            NotBefore = timeProvider.GetUtcNow().UtcDateTime,
            Expires = timeProvider.GetUtcNow().UtcDateTime.Add(_accessTokenOptions.Expiry),
            SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256)
        };

        var tokenHandler = new JsonWebTokenHandler();
        return new AccessToken(tokenHandler.CreateToken(tokenDescriptor));
    }

    private static RefreshToken GenerateRefreshToken()
    {
        var randomNumber = new byte[32];

        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);

        return new RefreshToken(Convert.ToBase64String(randomNumber));
    }
}
