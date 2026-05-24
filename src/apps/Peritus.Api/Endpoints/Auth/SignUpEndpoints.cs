using Peritus.Api.Extensions;
using Peritus.ApiContracts.Identity.Auth;
using Peritus.Identity.Features.Auth;

namespace Peritus.Api.Endpoints.Auth;

public static class SignUpEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app
            .MapGroup("/auth")
            .WithTags("Auth");

        group.MapPost("/signup", HandleSignUpAsync)
            .Produces<SignUpResponse>()
            .WithSummary("Sign up");
    }

    private static async Task<IResult> HandleSignUpAsync(
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
                AccessToken = x.AccessToken!.Value,
                RefreshToken = x.RefreshToken!.Value
            }
        );
    }
}
