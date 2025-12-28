using System.Data;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Peritus.Identity.RestContracts;
using Peritus.Identity.RestContracts.Auth;
using Peritus.IntegrationTests.Fixtures;
using Peritus.IntegrationTests.Types;
using Peritus.Types.Tokens;
using Refit;
using Xunit;

namespace Peritus.IntegrationTests.Abstractions;

public abstract class ApiTest(InfrastructureFixture fixture) : IAsyncLifetime
{
    protected PeritusApplicationFactory AppFactory { get; private set; } = null!;
    protected FakeTimeProvider TimeProvider { get; } = new();
    protected DateTime UtcNow => TimeProvider.GetUtcNow().UtcDateTime;

    public ValueTask InitializeAsync()
    {
        AppFactory = new PeritusApplicationFactory(
            services =>
            {
                ConfigureServicesInternal(services);
                ConfigureServices(services);
            },
            GetSettingsInternal().Concat(GetSettings()));

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await AppFactory.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    protected virtual Dictionary<string, string?> GetSettings()
    {
        return new Dictionary<string, string?>();
    }

    protected async Task<IDbConnection> CreateDbConnectionAsync(CancellationToken ct)
    {
        var connection = fixture.DataSource!.CreateConnection();
        await connection.OpenAsync(ct);
        return connection;
    }

    protected HttpClient CreateHttpClient()
    {
        return AppFactory.CreateDefaultClient();
    }

    protected T CreateRestClient<T>(AccessToken? accessToken = null)
    {
        var httpClient = CreateHttpClient();

        if (!string.IsNullOrEmpty(accessToken))
        {
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return RestService.For<T>(httpClient);
    }

    protected async Task<UserCredentials> CreateUserAsync(CancellationToken ct = default)
    {
        var credentials = UserCredentials.Create();

        var api = CreateRestClient<IIdentityApi>();
        using var response = await api.SignUpAsync(
            new SignUpRequest
            {
                Email = credentials.Email,
                Password = credentials.Password
            }, ct);

        // ReSharper disable once InvertIf
        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = response.Error?.Content;
            throw new InvalidOperationException($"Failed to create user: {errorMessage}");
        }

        return credentials;
    }

    protected async Task<AuthTokenPair> SignInAsync(UserCredentials testUser)
    {
        var api = CreateRestClient<IIdentityApi>();
        using var response = await api.SignInAsync(
            new SignInRequest
            {
                Email = testUser.Email,
                Password = testUser.Password
            });

        // ReSharper disable once InvertIf
        if (!response.IsSuccessStatusCode || response.Content is null)
        {
            var errorMessage = response.Error?.Content;
            throw new InvalidOperationException($"Failed to sign in: {errorMessage}");
        }

        return new AuthTokenPair(
            new AccessToken(response.Content.AccessToken),
            new RefreshToken(response.Content.RefreshToken)
        );
    }

    private void ConfigureServicesInternal(IServiceCollection services)
    {
        services.AddSingleton<TimeProvider>(TimeProvider);
    }

    private Dictionary<string, string?> GetSettingsInternal()
    {
        return new Dictionary<string, string?>
        {
            {
                "ConnectionStrings:db", fixture.DbConnectionString
            },
            {
                "ConnectionStrings:cache", fixture.CacheConnectionString
            },
            {
                "AccessTokenOptions:ExpirySeconds", "3600"
            },
            {
                "RefreshTokenOptions:ExpirySeconds", "86400"
            }
        };
    }
}
