using System.Security.Claims;
using Peritus.Api.Extensions;
using Peritus.Guard.Extensions;
using Peritus.Identity.Features.Accounts;

namespace Peritus.Api.Endpoints.Auth;

public static class SignOutEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app
            .MapGroup("/auth")
            .WithTags("Auth");

        group.MapPost("/signout", HandleSignOutAsync)
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .WithSummary("Sign out");
    }

    private static async Task<IResult> HandleSignOutAsync(
        SignOutFeature feature,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        var result = await feature.ExecuteAsync(
            new SignOutFeature.Context
            {
                AccessTokenId = principal.GetAccessTokenId()
            }, ct);

        return result.ToResult();
    }
}
