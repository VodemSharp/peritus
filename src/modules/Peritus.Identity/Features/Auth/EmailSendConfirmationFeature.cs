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

public class EmailSendConfirmationFeature(
    IUserService userService,
    IUserTokenService userTokenService,
    IMediator mediator,
    IOptions<IdentityOptions> identityOptions
)
{
    private readonly IdentityOptions _options = identityOptions.Value;

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/emails/send-confirmation", async (
                Request request,
                EmailSendConfirmationFeature feature,
                CancellationToken ct) =>
            {
                var result = await feature.ExecuteAsync(request, ct);
                return result.ToResult();
            })
            .Produces(StatusCodes.Status200OK)
            .WithTags("Emails")
            .WithSummary("Send email confirmation");
    }

    private async Task<FluentResult> ExecuteAsync(Request request, CancellationToken ct)
    {
        var user = await userService.FindByEmailAsync(request.Email, ct);

        if (user is null || user.EmailConfirmed)
        {
            // Return success even if user not found or already confirmed (idempotent, no enumeration)
            return FluentResult.Success();
        }

        var result = await userTokenService.CreateAsync(
            user.Id, UserTokenType.EmailConfirmation, _options.EmailConfirmationTokenExpiry, ct: ct);

        await mediator.SendAsync(new SendEmailCommand(
            user.Email.Value,
            "Confirm your email",
            $"Your confirmation code is: {result.RawToken}"), ct);

        return FluentResult.Success();
    }

    public class Request
    {
        [JsonPropertyName("email")] public required Email Email { get; set; }
    }
}
