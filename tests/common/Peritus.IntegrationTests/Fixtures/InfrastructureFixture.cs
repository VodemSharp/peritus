using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Projects;
using Xunit;

namespace Peritus.IntegrationTests.Fixtures;

public class InfrastructureFixture : IAsyncLifetime
{
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(120);

    private DistributedApplication App { get; set; } = null!;

    public string DbConnectionString { get; set; } = null!;
    public string CacheConnectionString { get; set; } = null!;

    public NpgsqlDataSource? DataSource { get; private set; }

    public async ValueTask InitializeAsync()
    {
        using var cts = new CancellationTokenSource(_defaultTimeout);
        var ct = cts.Token;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Peritus_AppHost>(ct);

        // Remove API and Web resources — tests create their own WebApplicationFactory
        var resourcesToRemove = appHost.Resources.Where(r => r.Name is "api" or "web").ToList();
        foreach (var resource in resourcesToRemove)
        {
            appHost.Resources.Remove(resource);
        }

        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            // Override the logging filters from the app's configuration
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
            // To output logs to the xUnit.net ITestOutputHelper, consider adding a package from https://www.nuget.org/packages?q=xunit+logging
        });

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        App = await appHost.BuildAsync(ct).WaitAsync(_defaultTimeout, ct);
        await App.StartAsync(ct).WaitAsync(_defaultTimeout, ct);

        await App.ResourceNotifications
            .WaitForResourceHealthyAsync("db", ct)
            .WaitAsync(_defaultTimeout, ct);

        await App.ResourceNotifications
            .WaitForResourceHealthyAsync("cache", ct)
            .WaitAsync(_defaultTimeout, ct);

        await App.ResourceNotifications
            .WaitForResourceAsync("migrator", KnownResourceStates.Finished, ct)
            .WaitAsync(_defaultTimeout, ct);

        DbConnectionString = (await App.GetConnectionStringAsync("db", ct).AsTask())!;
        CacheConnectionString = (await App.GetConnectionStringAsync("cache", ct).AsTask())!;

        DataSource = NpgsqlDataSource.Create(DbConnectionString);
    }

    public async ValueTask DisposeAsync()
    {
        if (DataSource != null)
        {
            await DataSource.DisposeAsync();
        }

        using var cts = new CancellationTokenSource(_defaultTimeout);
        await App.StopAsync(cts.Token);
        await App.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
