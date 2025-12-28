using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Peritus.Api;

namespace Peritus.IntegrationTests;

public class PeritusApplicationFactory(
    Action<IServiceCollection>? configureServices = null,
    IEnumerable<KeyValuePair<string, string?>>? settings = null) : WebApplicationFactory<IApiMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (settings != null)
        {
            foreach (var setting in settings)
            {
                builder.UseSetting(setting.Key, setting.Value);
            }
        }

        if (configureServices != null)
        {
            builder.ConfigureServices(configureServices);
        }
    }
}
