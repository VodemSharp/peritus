using System.Security.Claims;
using Peritus.Api.Extensions;
using Peritus.Identity.Features.Auth;
using Peritus.Identity.RestContracts.Auth;

namespace Peritus.Api.Endpoints.Auth;

public static class RefreshTokenEndpoint
{
    public static async Task<IResult> HandleAsync(
        RefreshTokenRequest request,
        RefreshTokenFeature feature,
        HttpContext httpContext,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        var result = await feature.ExecuteAsync(
            new RefreshTokenFeature.Context
            {
                AccessToken = request.AccessToken,
                RefreshToken = request.RefreshToken,
                IpAddress = httpContext.GetRemoteIpAddress(),
                UserAgent = httpContext.Request.GetUserAgent()
            }, ct);

        return result.ToResult(x =>
            new RefreshTokenResponse
            {
                AccessToken = x.AccessToken,
                RefreshToken = x.RefreshToken
            }
        );
    }
}
