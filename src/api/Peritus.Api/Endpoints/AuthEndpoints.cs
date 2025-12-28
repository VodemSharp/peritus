using Peritus.Api.Endpoints.Auth;
using Peritus.Identity.RestContracts.Auth;

namespace Peritus.Api.Endpoints;

public static class AuthEndpoints
{
    public static void Map(WebApplication app)
    {
        var authGroup = app
            .MapGroup("/auth")
            .WithTags("Auth");

        authGroup.MapPost("/signout", SignOutEndpoint.HandleAsync)
            .WithSummary("Sign out");

        authGroup.MapPost("/signin", SignInEndpoint.HandleAsync)
            .Produces<SignInResponse>()
            .WithSummary("Sign in");

        authGroup.MapPost("/refresh", RefreshTokenEndpoint.HandleAsync)
            .Produces<RefreshTokenResponse>()
            .WithSummary("Refresh token");

        authGroup.MapPost("/signup", SignUpEndpoint.HandleAsync)
            .Produces<SignUpResponse>()
            .WithSummary("Sign up");
    }
}
