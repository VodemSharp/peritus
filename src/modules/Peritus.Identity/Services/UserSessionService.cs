using Microsoft.Extensions.Options;
using Peritus.Identity.Options;
using Peritus.Identity.Persistence;
using Peritus.Identity.Persistence.Entities.Users;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Types.Identity.Users;
using Peritus.Types.Tokens;

namespace Peritus.Identity.Services;

public class UserSessionService(
    IdentityDbContext db,
    ITokenService tokenService,
    IOptions<RefreshTokenOptions> refreshTokenOptions,
    TimeProvider timeProvider
) : IUserSessionService
{
    private readonly RefreshTokenOptions _refreshTokenOptions = refreshTokenOptions.Value;

    public async Task<AuthTokenPair> CreateAsync(UserId userId, IpAddress? ip, UserAgent userAgent,
        UserSessionProvider tokensProvider, CancellationToken ct = default)
    {
        var accessTokenId = AccessTokenId.Create();
        var authTokens = await tokenService.GenerateTokensAsync(userId, accessTokenId, ct);
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        await db.UserSessions.AddAsync(
            new UserSession
            {
                UserId = userId,
                IpAddress = ip,
                UserAgent = userAgent,
                AccessTokenId = accessTokenId,
                RefreshToken = authTokens.RefreshToken,
                Status = UserSessionStatus.Confirmed,
                Provider = tokensProvider,
                ExpiredAt = utcNow.AddSeconds(_refreshTokenOptions.ExpirySeconds)
            }, ct);

        await db.SaveChangesAsync(ct);

        return authTokens;
    }
}
