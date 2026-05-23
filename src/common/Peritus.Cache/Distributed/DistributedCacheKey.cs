namespace Peritus.Cache.Distributed;

public readonly record struct DistributedCacheKey(string Value)
{
    public static implicit operator string(DistributedCacheKey key) => key.Value;

    public static DistributedCacheKey Create(string value)
    {
        return new DistributedCacheKey(value);
    }
}
