using System.Security.Claims;
using Peritus.Api.Extensions;
using Peritus.ApiContracts.Identity.Profile;
using Peritus.Guard.Extensions;
using Peritus.Identity.Features.Profile;
using Peritus.Types.Localization;

namespace Peritus.Api.Endpoints;

public static class ProfileEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app
            .MapGroup("/profiles")
            .WithTags("Profile")
            .RequireAuthorization();

        group.MapGet("/", HandleGetCurrentUserAsync)
            .Produces<GetCurrentUserResponse>()
            .WithSummary("Get current user");

        group.MapPut("/", HandleUpdateProfileAsync)
            .Produces(StatusCodes.Status200OK)
            .WithSummary("Update profile");
    }

    private static async Task<IResult> HandleGetCurrentUserAsync(
        ProfileGetFeature feature,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        var result = await feature.ExecuteAsync(
            new ProfileGetFeature.Context
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

    private static async Task<IResult> HandleUpdateProfileAsync(
        UpdateProfileRequest request,
        ProfileUpdateFeature feature,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        var result = await feature.ExecuteAsync(
            new ProfileUpdateFeature.Context
            {
                UserId = principal.GetUserId(),
                Culture = request.Culture is not null
                    ? new Culture(new CultureCode(request.Culture), new CultureName(request.Culture))
                    : null
            }, ct);

        return result.ToResult();
    }
}
