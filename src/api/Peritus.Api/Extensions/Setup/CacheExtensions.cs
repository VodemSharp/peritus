using Peritus.Cache;

namespace Peritus.Api.Extensions.Setup;

public static class CacheExtensions
{
    public static WebApplicationBuilder ConfigureCache(this WebApplicationBuilder builder)
    {
        builder.AddRedisDistributedCache("cache");
        builder.Services.AddDistributedCacheService();
        return builder;
    }
}
