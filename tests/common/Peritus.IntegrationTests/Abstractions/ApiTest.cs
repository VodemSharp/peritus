using System.Data;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Peritus.ApiContracts.Identity;
using Peritus.ApiContracts.Identity.Auth;
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
        if (!response.IsSuccessful)
        {
            var errorMessage = response.HasResponseError(out var apiException)
                ? apiException.Content
                : response.Error?.Message;
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
        if (!response.IsSuccessful)
        {
            var errorMessage = response.HasResponseError(out var apiException)
                ? apiException.Content
                : response.Error?.Message;
            throw new InvalidOperationException($"Failed to sign in: {errorMessage}");
        }

        return new AuthTokenPair(response.Content.AccessToken!.Value, response.Content.RefreshToken!.Value);
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
                "IdentityOptions:AccessTokenExpirySeconds", "3600"
            },
            {
                "IdentityOptions:RefreshTokenExpirySeconds", "86400"
            }
        };
    }
}
