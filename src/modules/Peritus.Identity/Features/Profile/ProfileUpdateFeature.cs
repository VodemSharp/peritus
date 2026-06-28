using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Peritus.AspNetCore.Extensions;
using Peritus.FluentResults;
using Peritus.Guard.Extensions;
using Peritus.Identity.Services.Abstractions;
using Peritus.Types.Identity.Users;
using Peritus.Types.Localization;

namespace Peritus.Identity.Features.Profile;

public class ProfileUpdateFeature(IUserService userService)
{
    public static void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/profiles", async (
                Request request,
                ProfileUpdateFeature feature,
                ClaimsPrincipal principal,
                CancellationToken ct) =>
            {
                request.UserId = principal.GetUserId();

                var result = await feature.ExecuteAsync(request, ct);
                return result.ToResult();
            })
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .WithTags("Profile");
    }

    private async Task<FluentResult> ExecuteAsync(Request request, CancellationToken ct)
    {
        var user = await userService.GetByIdAsync(request.UserId, ct);

        if (request.Culture is not null)
        {
            user.Culture = new CultureCode(request.Culture);
        }

        await userService.UpdateAsync(user, ct);

        return FluentResult.Success();
    }

    public class Request
    {
        [JsonIgnore] public UserId UserId { get; set; }
        [JsonPropertyName("culture")] public string? Culture { get; set; }
    }
}
