using System.Net.Http.Headers;
using System.Net.Http.Json;
using Peritus.ApiClients.Abstractions;
using Peritus.ApiContracts.Identity.Auth;
using Peritus.Types.Http;

namespace Peritus.ApiClients.DelegatingHanlers;

public class RefreshTokenDelegatingHandler(
    ITokenStorage tokenStorage,
    IHttpClientFactory httpClientFactory,
    TimeProvider timeProvider,
    HttpClientName refreshClientName) : DelegatingHandler
{
    private const string RefreshEndpoint = "/auth/refresh";
    private const string AuthenticationScheme = "Bearer";
    private static readonly TimeSpan _refreshWindow = TimeSpan.FromMinutes(1);

    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        await EnsureTokenFreshAsync(ct);

        var accessToken = tokenStorage.AccessToken;
        if (accessToken is { } token)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(AuthenticationScheme, token);
        }

        return await base.SendAsync(request, ct);
    }

    private async Task EnsureTokenFreshAsync(CancellationToken ct)
    {
        if (!tokenStorage.AccessToken.HasValue)
        {
            return;
        }

        var accessToken = tokenStorage.AccessToken.Value;

        var expiresAt = accessToken.GetExpiration();
        if (expiresAt is null)
        {
            return;
        }

        if (expiresAt.Value - timeProvider.GetUtcNow() > _refreshWindow)
        {
            return;
        }

        await _refreshLock.WaitAsync(ct);
        try
        {
            var currentToken = tokenStorage.AccessToken;
            if (currentToken != accessToken)
            {
                return;
            }

            var currentExpiresAt = currentToken.Value.GetExpiration();
            if (currentExpiresAt is null)
            {
                return;
            }

            if (currentExpiresAt.Value - timeProvider.GetUtcNow() > _refreshWindow)
            {
                return;
            }

            await TryRefreshAsync(ct);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task TryRefreshAsync(CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient(refreshClientName.Value);

            var request = new RefreshTokenRequest
            {
                AccessToken = tokenStorage.AccessToken!.Value,
                RefreshToken = tokenStorage.RefreshToken!.Value
            };

            var response = await client.PostAsJsonAsync(RefreshEndpoint, request, ct);

            if (!response.IsSuccessStatusCode)
            {
                tokenStorage.ClearTokens();
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>(ct);
            if (result is null)
            {
                tokenStorage.ClearTokens();
                return;
            }

            tokenStorage.SetTokens(result.AccessToken, result.RefreshToken);
        }
        catch
        {
            tokenStorage.ClearTokens();
        }
    }
}
