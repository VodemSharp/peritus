using Microsoft.Extensions.DependencyInjection;
using Peritus.Persistence.Interceptors;

namespace Peritus.Persistence.Extensions;

public static class InterceptorExtensions
{
    public static IServiceCollection AddInterceptors(this IServiceCollection services)
    {
        services.AddTransient<PerformanceInterceptor>();
        services.AddTransient<TimestampInterceptor>();
        return services;
    }
}
