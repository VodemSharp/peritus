using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Peritus.AspNetCore.Extensions;
using Peritus.FluentResults;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Messages.Notification;
using Peritus.Messaging.Abstractions;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.Features.Auth;

public class PasswordResetSendFeature(
    IUserService userService,
    IUserTokenService userTokenService,
    IMediator mediator,
    IOptions<IdentityOptions> identityOptions)
{
    private readonly IdentityOptions _options = identityOptions.Value;

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/passwords/forgot", async (
                Request request,
                PasswordResetSendFeature feature,
                CancellationToken ct) =>
            {
                var result = await feature.ExecuteAsync(request, ct);
                return result.ToResult();
            })
            .Produces(StatusCodes.Status200OK)
            .WithTags("Passwords")
            .WithSummary("Send password reset");
    }

    private async Task<FluentResult> ExecuteAsync(Request request, CancellationToken ct)
    {
        var user = await userService.FindByEmailAsync(request.Email, ct);

        if (user is null)
        {
            // Return success to prevent account enumeration
            return FluentResult.Success();
        }

        var result = await userTokenService.CreateAsync(
            user.Id, UserTokenType.PasswordReset, _options.PasswordResetTokenExpiry, ct: ct);

        await mediator.SendAsync(new SendEmailCommand(
            user.Email.Value,
            "Reset your password",
            $"Your password reset code is: {result.RawToken}"), ct);

        return FluentResult.Success();
    }

    public class Request
    {
        [JsonPropertyName("email")] public required Email Email { get; set; }
    }
}
