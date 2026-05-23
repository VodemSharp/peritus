namespace Peritus.Cache.Distributed;

public interface IDistributedCacheService
{
    Task<string?> GetStringAsync(DistributedCacheKey key, CancellationToken ct = default);

    Task SetStringAsync(DistributedCacheKey key, string value, TimeSpan? expiry = null, CancellationToken ct = default);

    Task RemoveAsync(DistributedCacheKey key, CancellationToken ct = default);

    Task<T?> GetAsync<T>(DistributedCacheKey key, CancellationToken ct = default);

    Task SetAsync<T>(DistributedCacheKey key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
}
