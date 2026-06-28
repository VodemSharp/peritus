using Microsoft.Extensions.DependencyInjection;
using Peritus.ApiClients.Abstractions;
using Peritus.ApiClients.DelegatingHandlers;
using Peritus.ApiContracts.Identity;
using Peritus.Types.Http;
using Refit;

namespace Peritus.ApiClients.Extensions;

public static class IdentityApiClientExtensions
{
    public static IServiceCollection AddIdentityApiClient(
        this IServiceCollection services, Uri baseAddress, HttpClientName refreshClientName)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddHttpClient(refreshClientName.Value)
            .ConfigureHttpClient(client => client.BaseAddress = baseAddress);

        services.AddTransient<RefreshTokenDelegatingHandler>(sp => new RefreshTokenDelegatingHandler(
            sp.GetRequiredService<ITokenStorage>(),
            sp.GetRequiredService<IHttpClientFactory>(),
            sp.GetRequiredService<TimeProvider>(),
            refreshClientName));

        services.AddRefitClient<IIdentityApi>()
            .ConfigureHttpClient(client => client.BaseAddress = baseAddress)
            .AddHttpMessageHandler<RefreshTokenDelegatingHandler>();

        return services;
    }
}
