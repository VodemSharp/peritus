using System.Security.Claims;
using Peritus.Identity.Features.Auth;
using Peritus.Identity.RestContracts.Auth;

namespace Peritus.Api.Endpoints.Auth;

public static class SignOutEndpoint
{
    public static async Task HandleAsync(
        SignOutRequest request,
        SignOutFeature feature,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        await feature.ExecuteAsync(
            new SignOutFeature.Context
            {
                AccessToken = request.AccessToken
            }, ct);
    }
}
