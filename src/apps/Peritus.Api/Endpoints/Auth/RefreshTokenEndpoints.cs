using Peritus.Api.Extensions;
using Peritus.ApiContracts.Identity.Auth;
using Peritus.Identity.Features.Auth;

namespace Peritus.Api.Endpoints.Auth;

public static class RefreshTokenEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app
            .MapGroup("/auth")
            .WithTags("Auth");

        group.MapPost("/refresh", HandleRefreshTokenAsync)
            .Produces<RefreshTokenResponse>()
            .WithSummary("Refresh token");
    }

    private static async Task<IResult> HandleRefreshTokenAsync(
        RefreshTokenRequest request,
        TokenRefreshFeature feature,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await feature.ExecuteAsync(
            new TokenRefreshFeature.Context
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
