using Microsoft.Extensions.Caching.Distributed;
using Peritus.Guard.Services.Abstractions;
using Peritus.Types.Tokens;

namespace Peritus.Guard.Services;

public class TokenRevoker(IDistributedCache cache) : ITokenRevoker
{
    public async Task<bool> IsRevokedAsync(AccessTokenId tokenId, CancellationToken ct = default)
    {
        return await cache.GetStringAsync(GetKey(tokenId), ct) != null;
    }

    public async Task RevokeAsync(AccessTokenId tokenId, TimeSpan expiration, CancellationToken ct = default)
    {
        if (!await IsRevokedAsync(tokenId, ct))
        {
            await cache.SetStringAsync(GetKey(tokenId), string.Empty,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration
                }, ct);
        }
    }

    private static string GetKey(AccessTokenId tokenId)
    {
        return $"tokens:{tokenId}:deactivated";
    }
}
