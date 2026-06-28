using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Peritus.AspNetCore.Extensions;
using Peritus.FluentResults;
using Peritus.Guard.Extensions;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Messages.Notification;
using Peritus.Messaging.Abstractions;
using Peritus.Types.Identity.Users;

namespace Peritus.Identity.Features.Accounts;

public class PhoneNumberSendVerificationFeature(
    IUserService userService,
    IUserTokenService userTokenService,
    IMediator mediator,
    IOptions<IdentityOptions> identityOptions)
{
    private readonly IdentityOptions _options = identityOptions.Value;

    public static void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/accounts/phones/send-verification", async (
                Request request,
                PhoneNumberSendVerificationFeature feature,
                ClaimsPrincipal principal,
                CancellationToken ct) =>
            {
                request.UserId = principal.GetUserId();

                var result = await feature.ExecuteAsync(request, ct);
                return result.ToResult();
            })
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .WithTags("Phones");
    }

    private async Task<FluentResult> ExecuteAsync(Request request, CancellationToken ct)
    {
        var user = await userService.GetByIdAsync(request.UserId, ct);

        if (!PhoneNumber.IsValid(request.PhoneNumber))
        {
            return FluentResult.ValidationProblem(nameof(request.PhoneNumber), "Invalid phone number format.");
        }

        if (user.PhoneNumber == request.PhoneNumber && user.PhoneNumberConfirmed)
        {
            return FluentResult.Success();
        }

        var tokenResult = await userTokenService.CreateAsync(
            user.Id,
            UserTokenType.PhoneNumberVerification,
            _options.PhoneNumberVerificationTokenExpiry,
            request.PhoneNumber,
            ct);

        await mediator.SendAsync(new SendSmsCommand(
            request.PhoneNumber,
            $"Your verification code is: {tokenResult.RawToken}"), ct);

        return FluentResult.Success();
    }

    public class Request
    {
        [JsonIgnore] public UserId UserId { get; set; }
        [JsonPropertyName("phoneNumber")] public required PhoneNumber PhoneNumber { get; set; }
    }
}
