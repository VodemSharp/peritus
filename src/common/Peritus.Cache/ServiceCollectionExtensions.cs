using Microsoft.Extensions.DependencyInjection;
using Peritus.Cache.Distributed;

namespace Peritus.Cache;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDistributedCacheService(this IServiceCollection services)
    {
        services.AddSingleton<IDistributedCacheService, DistributedCacheService>();
        return services;
    }
}
