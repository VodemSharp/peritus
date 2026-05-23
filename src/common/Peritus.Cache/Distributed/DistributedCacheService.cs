using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Peritus.Cache.Distributed;

public class DistributedCacheService(IDistributedCache cache) : IDistributedCacheService
{
    public async Task<string?> GetStringAsync(DistributedCacheKey key, CancellationToken ct = default)
    {
        return await cache.GetStringAsync(key, ct);
    }

    public async Task SetStringAsync(DistributedCacheKey key, string value, TimeSpan? expiry = null,
        CancellationToken ct = default)
    {
        var options = expiry.HasValue
            ? new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry.Value
            }
            : new DistributedCacheEntryOptions();

        await cache.SetStringAsync(key, value, options, ct);
    }

    public async Task RemoveAsync(DistributedCacheKey key, CancellationToken ct = default)
    {
        await cache.RemoveAsync(key, ct);
    }

    public async Task<T?> GetAsync<T>(DistributedCacheKey key, CancellationToken ct = default)
    {
        var json = await GetStringAsync(key, ct);
        return json is null ? default : JsonSerializer.Deserialize<T>(json);
    }

    public async Task SetAsync<T>(DistributedCacheKey key, T value, TimeSpan? expiry = null,
        CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value);
        await SetStringAsync(key, json, expiry, ct);
    }
}
