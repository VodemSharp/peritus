using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Peritus.AspNetCore.Extensions;
using Peritus.FluentResults;
using Peritus.Guard.Extensions;
using Peritus.Identity.Persistence;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.Features.Profile;

public class ProfileGetFeature(IdentityDbContext db)
{
    public static void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/profiles", async (
                ProfileGetFeature feature,
                ClaimsPrincipal principal,
                CancellationToken ct) =>
            {
                var request = new Request
                {
                    UserId = principal.GetUserId()
                };

                var result = await feature.ExecuteAsync(request, ct);
                return result.ToResult();
            })
            .RequireAuthorization()
            .Produces<Response>()
            .WithTags("Profile");
    }

    private async Task<FluentResult<Response>> ExecuteAsync(Request request, CancellationToken ct)
    {
        var result = await db.Users
            .Where(x => x.Id == request.UserId)
            .Select(x => new Response
            {
                Email = x.Email
            })
            .SingleAsync(ct);

        return FluentResult<Response>.Success(result);
    }

    public class Request
    {
        [JsonIgnore] public UserId UserId { get; set; }
    }

    public class Response
    {
        [JsonPropertyName("email")] public required string Email { get; set; }
    }
}
