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
using Peritus.Identity.Types;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.Features.Accounts;

public class SessionListFeature(IdentityDbContext db)
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/accounts/sessions", async (
                SessionListFeature feature,
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
            .Produces<List<Response>>()
            .RequireAuthorization()
            .WithTags("Sessions")
            .WithSummary("List sessions");
    }

    private async Task<FluentResult<List<Response>>> ExecuteAsync(Request request, CancellationToken ct)
    {
        var sessions = await db.UserSessions
            .Where(x => x.UserId == request.UserId && x.Status == UserSessionStatus.Confirmed)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new Response
            {
                Id = x.Id,
                IpAddress = x.IpAddress,
                UserAgent = x.UserAgent,
                ExpiredAt = x.ExpiredAt,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(ct);

        return FluentResult<List<Response>>.Success(sessions);
    }

    public class Request
    {
        [JsonIgnore] public UserId UserId { get; set; }
    }

    public class Response
    {
        [JsonPropertyName("id")] public required UserSessionId Id { get; set; }
        [JsonPropertyName("ipAddress")] public required IpAddress? IpAddress { get; set; }
        [JsonPropertyName("userAgent")] public required UserAgent UserAgent { get; set; }
        [JsonPropertyName("expiredAt")] public required DateTime ExpiredAt { get; set; }
        [JsonPropertyName("createdAt")] public required DateTime CreatedAt { get; set; }
    }
}
