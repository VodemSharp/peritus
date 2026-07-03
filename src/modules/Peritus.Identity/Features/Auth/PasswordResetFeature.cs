using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Peritus.AspNetCore.Extensions;
using Peritus.FluentResults;
using Peritus.Identity.Persistence;
using Peritus.Identity.Persistence.Entities.Users;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Persistence.Extensions;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.Features.Auth;

public class PasswordResetFeature(
    IUserService userService,
    IUserTokenService userTokenService,
    IPasswordHasher<User> passwordHasher,
    TimeProvider timeProvider,
    IdentityDbContext db)
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/passwords/reset", async (
                Request request,
                PasswordResetFeature feature,
                CancellationToken ct) =>
            {
                var result = await feature.ExecuteAsync(request, ct);
                return result.ToResult();
            })
            .Produces(StatusCodes.Status200OK)
            .WithTags("Passwords")
            .WithSummary("Reset password");
    }

    private async Task<FluentResult> ExecuteAsync(Request request, CancellationToken ct)
    {
        var user = await userService.FindByEmailAsync(request.Email, ct);

        if (user is null)
        {
            return FluentResult.ValidationProblem(
                nameof(request.Token),
                IdentityErrorCodes.InvalidPasswordResetToken);
        }

        var result = await userTokenService.RedeemAsync(user.Id, UserTokenType.PasswordReset, request.Token, ct);

        if (!result.IsSuccess)
        {
            return FluentResult.ValidationProblem(
                nameof(request.Token),
                IdentityErrorCodes.InvalidPasswordResetToken);
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
        [JsonPropertyName("email")] public required Email Email { get; set; }
        [JsonPropertyName("token")] public required string Token { get; set; }
        [JsonPropertyName("newPassword")] public required Password NewPassword { get; set; }
    }
}
