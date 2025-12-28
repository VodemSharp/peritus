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
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);

    private DistributedApplication App { get; set; } = null!;

    public string DbConnectionString { get; set; } = null!;
    public string CacheConnectionString { get; set; } = null!;

    public NpgsqlDataSource? DataSource { get; private set; }

    public async ValueTask InitializeAsync()
    {
        const string apiResourceName = "api";

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Peritus_AppHost>(_ct);
        var resource = appHost.Resources.Single(r => r.Name == apiResourceName);
        appHost.Resources.Remove(resource);

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

        App = await appHost.BuildAsync(_ct).WaitAsync(_defaultTimeout, _ct);
        await App.StartAsync(_ct).WaitAsync(_defaultTimeout, _ct);

        await App.ResourceNotifications
            .WaitForResourceHealthyAsync("db", _ct)
            .WaitAsync(_defaultTimeout, _ct);

        await App.ResourceNotifications
            .WaitForResourceHealthyAsync("cache", _ct)
            .WaitAsync(_defaultTimeout, _ct);

        await App.ResourceNotifications
            .WaitForResourceAsync("identity-migrator", KnownResourceStates.Finished, _ct)
            .WaitAsync(_defaultTimeout, _ct);

        DbConnectionString = (await App.GetConnectionStringAsync("db", _ct).AsTask())!;
        CacheConnectionString = (await App.GetConnectionStringAsync("cache", _ct).AsTask())!;

        DataSource = NpgsqlDataSource.Create(DbConnectionString);
    }

    public async ValueTask DisposeAsync()
    {
        if (DataSource != null)
        {
            await DataSource.DisposeAsync();
        }

        await App.StopAsync(_ct);
        await App.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
