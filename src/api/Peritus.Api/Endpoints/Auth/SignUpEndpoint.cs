using Peritus.Api.Extensions;
using Peritus.Identity.Features.Auth;
using Peritus.Identity.RestContracts.Auth;

namespace Peritus.Api.Endpoints.Auth;

public static class SignUpEndpoint
{
    public static async Task<IResult> HandleAsync(
        SignUpRequest request,
        SignUpFeature feature,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await feature.ExecuteAsync(
            new SignUpFeature.Context
            {
                Email = request.Email,
                Password = request.Password,
                IpAddress = httpContext.GetRemoteIpAddress(),
                UserAgent = httpContext.Request.GetUserAgent()
            }, ct);

        return result.ToResult(x =>
            new SignUpResponse
            {
                AccessToken = x.AccessToken,
                RefreshToken = x.RefreshToken
            }
        );
    }
}
