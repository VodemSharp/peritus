using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;
using Peritus.ApiClients.Abstractions;

namespace Peritus.Web.Services;

public class CookieAuthenticationStateProvider(ITokenStorage tokenStorage) : AuthenticationStateProvider
{
    private const string AuthenticationType = "Bearer";
    private static readonly JsonWebTokenHandler _tokenHandler = new();

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = tokenStorage.AccessToken;
        if (token is null)
        {
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        }

        var principal = ParseClaims(token.Value.Value);

        return Task.FromResult(principal is null
            ? new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))
            : new AuthenticationState(principal));
    }

    private static ClaimsPrincipal? ParseClaims(string token)
    {
        try
        {
            var jwt = _tokenHandler.ReadJsonWebToken(token);
            var identity = new ClaimsIdentity(jwt.Claims, AuthenticationType);
            return new ClaimsPrincipal(identity);
        }
        catch
        {
            return null;
        }
    }
}
