using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Peritus.AspNetCore.Extensions;
using Peritus.FluentResults;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.Features.Auth;

public class EmailConfirmFeature(
    IUserService userService,
    IUserTokenService userTokenService)
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/emails/confirm", async (
                Request request,
                EmailConfirmFeature feature,
                CancellationToken ct) =>
            {
                var result = await feature.ExecuteAsync(request, ct);
                return result.ToResult();
            })
            .Produces(StatusCodes.Status200OK)
            .WithTags("Emails")
            .WithSummary("Confirm email");
    }

    private async Task<FluentResult> ExecuteAsync(Request request, CancellationToken ct)
    {
        var user = await userService.FindByEmailAsync(request.Email, ct);

        if (user is null)
        {
            return FluentResult.ValidationProblem(nameof(request.Token), "Invalid or expired token.");
        }

        var result = await userTokenService.RedeemAsync(user.Id, UserTokenType.EmailConfirmation, request.Token, ct);

        if (!result.IsSuccess)
        {
            return FluentResult.ValidationProblem(nameof(request.Token), "Invalid or expired token.");
        }

        user.EmailConfirmed = true;
        await userService.UpdateAsync(user, ct);

        return FluentResult.Success();
    }

    public class Request
    {
        [JsonPropertyName("email")] public required Email Email { get; set; }
        [JsonPropertyName("token")] public required string Token { get; set; }
    }
}
