using Microsoft.EntityFrameworkCore;
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
    IOptions<IdentityOptions> identityOptions,
    TimeProvider timeProvider
) : IUserSessionService
{
    private readonly IdentityOptions _options = identityOptions.Value;

    public async Task<SessionCreationResult> CreateAsync(UserId userId, IpAddress? ip, UserAgent userAgent,
        UserExternalLoginId? externalLoginId = null, CancellationToken ct = default)
    {
        var accessTokenId = AccessTokenId.Create();
        var authTokens = await tokenService.GenerateTokensAsync(userId, accessTokenId, ct);
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var expiredAt = utcNow.AddSeconds(_options.RefreshTokenExpirySeconds);

        await db.UserSessions.AddAsync(
            new UserSession
            {
                UserId = userId,
                IpAddress = ip,
                UserAgent = userAgent,
                AccessTokenId = accessTokenId,
                RefreshToken = authTokens.RefreshToken,
                Status = UserSessionStatus.Confirmed,
                ExternalLoginId = externalLoginId,
                ExpiredAt = expiredAt
            }, ct);

        await db.SaveChangesAsync(ct);

        return new SessionCreationResult(accessTokenId, authTokens, expiredAt);
    }

    public async Task<UserSession?> FindByAccessTokenIdAsync(AccessTokenId accessTokenId, CancellationToken ct = default)
    {
        return await db.UserSessions
            .SingleOrDefaultAsync(s => s.AccessTokenId == accessTokenId, ct);
    }
}
