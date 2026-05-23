using Microsoft.Extensions.Options;
using Peritus.Cache.Distributed;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Types.Tokens;

namespace Peritus.Identity.Services;

public class SessionValidator(
    IDistributedCacheService cache,
    IUserSessionService userSessionService,
    TimeProvider timeProvider,
    IOptions<IdentityOptions> identityOptions) : ISessionValidator
{
    private readonly IdentityOptions _options = identityOptions.Value;

    public async Task<bool> IsValidAsync(AccessTokenId accessTokenId, CancellationToken ct = default)
    {
        // Fast path: check cache
        var cachedStatus = await cache.GetStringAsync(IdentityCacheKeys.Session(accessTokenId), ct);
        if (cachedStatus is not null)
        {
            return cachedStatus == UserSessionStatus.Confirmed;
        }

        // Cache miss: check database
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var session = await userSessionService.FindByAccessTokenIdAsync(accessTokenId, ct);

        if (session is null || session.Status != UserSessionStatus.Confirmed || session.ExpiredAt < utcNow)
        {
            return false;
        }

        // Populate cache for subsequent requests
        await SetAsync(accessTokenId, session.ExpiredAt, ct);
        return true;
    }

    public async Task SetAsync(AccessTokenId accessTokenId, DateTime expiredAt, CancellationToken ct = default)
    {
        var ttl = expiredAt - timeProvider.GetUtcNow().UtcDateTime;
        if (ttl <= TimeSpan.Zero)
        {
            return;
        }

        // Clamp TTL to access token expiry to avoid keeping terminated sessions in cache too long
        var maxTtl = _options.AccessTokenExpiry;
        if (ttl > maxTtl)
        {
            ttl = maxTtl;
        }

        await cache.SetStringAsync(IdentityCacheKeys.Session(accessTokenId), UserSessionStatus.Confirmed, ttl, ct);
    }

    public async Task RemoveAsync(AccessTokenId accessTokenId, CancellationToken ct = default)
    {
        await cache.RemoveAsync(IdentityCacheKeys.Session(accessTokenId), ct);
    }
}
