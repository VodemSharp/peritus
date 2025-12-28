using Peritus.Api.Endpoints.Users;
using Peritus.Identity.RestContracts.Users;

namespace Peritus.Api.Endpoints;

public static class UserEndpoints
{
    public static void Map(WebApplication app)
    {
        var authGroup = app
            .MapGroup("/users")
            .WithTags("Users")
            .RequireAuthorization();

        authGroup.MapGet("/me", GetCurrentUserEndpoint.HandleAsync)
            .Produces<GetCurrentUserResponse>()
            .WithSummary("Get current user");
    }
}
