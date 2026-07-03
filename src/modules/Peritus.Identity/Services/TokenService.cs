using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Peritus.Cache.Distributed;
using Peritus.FluentResults;
using Peritus.Guard.Claims;
using Peritus.Identity.Persistence;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Types.Identity.Users;
using Peritus.Types.Tokens;

namespace Peritus.Identity.Services;

public class TokenService(
    TimeProvider timeProvider,
    IdentityDbContext db,
    IDistributedCacheService cache,
    IOptions<IdentityOptions> identityOptions,
    IUserService userService
) : ITokenService
{
    private readonly IdentityOptions _options = identityOptions.Value;

    public async Task<FluentResult<ClaimsPrincipal>> GetPrincipalFromExpiredTokenAsync(AccessToken token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = _options.JwtAudience,
            ValidateIssuer = true,
            ValidIssuer = _options.JwtIssuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.JwtKey)),
            ValidateLifetime = false
        };

        var tokenHandler = new JsonWebTokenHandler();
        var result = await tokenHandler.ValidateTokenAsync(token, tokenValidationParameters);

        if (!result.IsValid)
        {
            return FluentResult<ClaimsPrincipal>.ValidationMessage(IdentityErrorCodes.InvalidAccessToken);
        }

        var jsonToken = tokenHandler.ReadJsonWebToken(token);
        return !jsonToken.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase)
            ? FluentResult<ClaimsPrincipal>.ValidationMessage(IdentityErrorCodes.InvalidAccessToken)
            : FluentResult<ClaimsPrincipal>.Success(new ClaimsPrincipal(result.ClaimsIdentity));
    }

    public async Task<AuthTokenPair> GenerateTokensAsync(
        UserId userId, AccessTokenId accessTokenId, CancellationToken ct = default)
    {
        var userRoles = await db.UserRoles
            .Include(x => x.Role)
            .Where(x => x.UserId == userId)
            .ToListAsync(ct);

        var user = await userService.GetByIdAsync(userId, ct);

        var claims = userRoles
            .Select(x => new Claim(ClaimTypes.Role, x.Role!.Name))
            .ToList();

        claims.Add(new Claim(CustomClaimTypes.Email, user.Email.Value));

        return new AuthTokenPair
        {
            AccessToken = GenerateAccessToken(userId, accessTokenId, claims),
            RefreshToken = RefreshToken.Generate()
        };
    }

    public async Task<string> GenerateTwoFactorTokenAsync(UserId userId, CancellationToken ct = default)
    {
        var token = SecureToken.Generate().Value;
        var cacheKey = IdentityCacheKeys.TwoFactorToken(token);

        await cache.SetStringAsync(
            cacheKey,
            userId.Value.ToString(),
            _options.TwoFactorTokenExpiry,
            ct);

        return token;
    }

    public async Task<FluentResult<UserId>> ValidateTwoFactorTokenAsync(string token, CancellationToken ct = default)
    {
        var cacheKey = IdentityCacheKeys.TwoFactorToken(token);
        var userIdValue = await cache.GetStringAsync(cacheKey, ct);

        if (string.IsNullOrEmpty(userIdValue) || !Guid.TryParse(userIdValue, out var userIdGuid))
        {
            return FluentResult<UserId>.ValidationMessage(IdentityErrorCodes.InvalidTwoFactorToken);
        }

        return FluentResult<UserId>.Success(new UserId(userIdGuid));
    }

    private AccessToken GenerateAccessToken(UserId userId, AccessTokenId accessTokenId, List<Claim> claims)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.JwtKey));

        var jwtClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, accessTokenId.Value.ToString()),
            new(CustomClaimTypes.UserId, userId.Value.ToString())
        }.Union(claims);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(jwtClaims),
            Issuer = _options.JwtIssuer,
            Audience = _options.JwtAudience,
            NotBefore = timeProvider.GetUtcNow().UtcDateTime,
            Expires = timeProvider.GetUtcNow().UtcDateTime.Add(_options.AccessTokenExpiry),
            SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256)
        };

        var tokenHandler = new JsonWebTokenHandler();
        return new AccessToken(tokenHandler.CreateToken(tokenDescriptor));
    }
}
