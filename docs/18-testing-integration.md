# Testing: xUnit integration with Aspire and WebApplicationFactory

Integration tests run against real infra (Postgres, Valkey) provisioned by Aspire, while the API is hosted via `WebApplicationFactory` with DI overrides (e.g., fake time).

## Aspire testing host
- `InfrastructureFixture` spins up the distributed application, removes the API resource (we replace it with a factory), starts resources, waits for health, and exposes connection strings:
  ```csharp
  var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Peritus_AppHost>(_ct);
  var resource = appHost.Resources.Single(r => r.Name == apiResourceName);
  appHost.Resources.Remove(resource);
  // logging + resilience tweaks omitted for brevity
  App = await appHost.BuildAsync(_ct).WaitAsync(_defaultTimeout, _ct);
  await App.StartAsync(_ct).WaitAsync(_defaultTimeout, _ct);
  await App.ResourceNotifications.WaitForResourceHealthyAsync("db", _ct);
  await App.ResourceNotifications.WaitForResourceHealthyAsync("cache", _ct);
  await App.ResourceNotifications.WaitForResourceAsync("identity-migrator", KnownResourceStates.Finished, _ct);
  DbConnectionString = (await App.GetConnectionStringAsync("db", _ct).AsTask())!;
  CacheConnectionString = (await App.GetConnectionStringAsync("cache", _ct).AsTask())!;
  ```
  @/tests/common/Peritus.IntegrationTests/Fixtures/InfrastructureFixture.cs#24-65
- Cleans up Npgsql data source and stops the app on dispose.@/tests/common/Peritus.IntegrationTests/Fixtures/InfrastructureFixture.cs#67-75

## WebApplicationFactory with DI overrides
- `PeritusApplicationFactory` applies custom settings and service configuration for tests:
  ```csharp
  public class PeritusApplicationFactory(..., IEnumerable<KeyValuePair<string, string?>>? settings = null)
      : WebApplicationFactory<IApiMarker>
  {
      protected override void ConfigureWebHost(IWebHostBuilder builder)
      {
          if (settings != null)
              foreach (var setting in settings) builder.UseSetting(setting.Key, setting.Value);

          if (configureServices != null)
              builder.ConfigureServices(configureServices);
      }
  }
  ```
  @/tests/common/Peritus.IntegrationTests/PeritusApplicationFactory.cs#8-25

## Base test class with FakeTimeProvider
- `ApiTest` sets up a `FakeTimeProvider` override and supplies settings (connection strings, token expiries). It also exposes helpers to create Refit clients and seed users:
  ```csharp
  protected FakeTimeProvider TimeProvider { get; } = new();
  public ValueTask InitializeAsync()
  {
      AppFactory = new PeritusApplicationFactory(
          services =>
          {
              ConfigureServicesInternal(services); // adds TimeProvider as singleton TimeProvider
              ConfigureServices(services);
          },
          GetSettingsInternal().Concat(GetSettings()));
      return ValueTask.CompletedTask;
  }

  protected T CreateRestClient<T>(AccessToken? accessToken = null)
  {
      var httpClient = CreateHttpClient();
      if (!string.IsNullOrEmpty(accessToken))
          httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
      return RestService.For<T>(httpClient);
  }
  ```
  @/tests/common/Peritus.IntegrationTests/Abstractions/ApiTest.cs#15-141
- `GetSettingsInternal` injects DB/cache connection strings and token expiries from the fixture so the app under test uses real resources.@/tests/common/Peritus.IntegrationTests/Abstractions/ApiTest.cs#124-139

## How to write a new integration test
1) Derive from `ApiTest` and inject `InfrastructureFixture` via xUnit fixture mechanism.
2) Use `CreateUserAsync` and `SignInAsync` helpers to provision test users and tokens.
3) Use `TimeProvider` to control expiry-related scenarios deterministically.
4) Use `CreateRestClient<IIdentityApi>()` to call endpoints via typed Refit clients with optional bearer tokens.
