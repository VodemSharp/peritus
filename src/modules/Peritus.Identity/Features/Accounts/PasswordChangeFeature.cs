using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Peritus.AspNetCore.Extensions;
using Peritus.FluentResults;
using Peritus.Guard.Extensions;
using Peritus.Identity.Persistence;
using Peritus.Identity.Persistence.Entities.Users;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Persistence.Extensions;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.Features.Accounts;

public class PasswordChangeFeature(
    IUserService userService,
    IPasswordHasher<User> passwordHasher,
    TimeProvider timeProvider,
    IdentityDbContext db)
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/accounts/passwords/change", async (
                Request request,
                PasswordChangeFeature feature,
                ClaimsPrincipal principal,
                CancellationToken ct) =>
            {
                request.UserId = principal.GetUserId();

                var result = await feature.ExecuteAsync(request, ct);
                return result.ToResult();
            })
            .Produces(StatusCodes.Status200OK)
            .RequireAuthorization()
            .WithTags("Passwords")
            .WithSummary("Change password");
    }

    private async Task<FluentResult> ExecuteAsync(Request request, CancellationToken ct)
    {
        var user = await userService.GetByIdAsync(request.UserId, ct);

        var verifyResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);

        if (verifyResult == PasswordVerificationResult.Failed)
        {
            return FluentResult.ValidationProblem(nameof(request.CurrentPassword), "Current password is incorrect.");
        }

        await db.ExecuteInTransactionAsync(async () =>
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
            db.Users.Update(user);
            await db.SaveChangesAsync(ct);

            await db.UserSessions
                .Where(s => s.UserId == user.Id && s.Status == UserSessionStatus.Confirmed)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(s => s.Status, UserSessionStatus.Terminated)
                        .SetProperty(s => s.UpdatedAt, timeProvider.GetUtcNow().UtcDateTime),
                    ct);
        }, ct);

        return FluentResult.Success();
    }

    public class Request
    {
        [JsonIgnore] public UserId UserId { get; set; }
        [JsonPropertyName("currentPassword")] public required string CurrentPassword { get; set; }
        [JsonPropertyName("newPassword")] public required string NewPassword { get; set; }
    }
}
