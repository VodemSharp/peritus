using System.Security.Claims;
using Peritus.Api.Extensions;
using Peritus.Guard.Extensions;
using Peritus.Identity.Features.Users;
using Peritus.Identity.RestContracts.Users;

namespace Peritus.Api.Endpoints.Users;

public static class GetCurrentUserEndpoint
{
    public static async Task<IResult> HandleAsync(
        GetCurrentUserFeature feature,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        var result = await feature.ExecuteAsync(
            new GetCurrentUserFeature.Context
            {
                UserId = principal.GetUserId()
            }, ct);

        return result.ToResult(x =>
            new GetCurrentUserResponse
            {
                Email = x.Email
            }
        );
    }
}
