using Peritus.Api.Extensions;
using Peritus.Identity.Features.Auth;
using Peritus.Identity.RestContracts.Auth;

namespace Peritus.Api.Endpoints.Auth;

public static class SignInEndpoint
{
    public static async Task<IResult> HandleAsync(
        SignInRequest request,
        SignInFeature feature,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await feature.ExecuteAsync(
            new SignInFeature.Context
            {
                Email = request.Email,
                Password = request.Password,
                IpAddress = httpContext.GetRemoteIpAddress(),
                UserAgent = httpContext.Request.GetUserAgent()
            }, ct);

        return result.ToResult(x =>
            new SignInResponse
            {
                AccessToken = x.AccessToken,
                RefreshToken = x.RefreshToken
            }
        );
    }
}
