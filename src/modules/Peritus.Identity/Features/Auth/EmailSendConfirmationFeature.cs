using Microsoft.Extensions.Options;
using Peritus.FluentResults;
using Peritus.Identity.Options;
using Peritus.Identity.Services.Abstractions;
using Peritus.Messaging;
using Peritus.Messages.Notification;
using Peritus.Identity.Types;
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

    public async Task<FluentResult> ExecuteAsync(Context context, CancellationToken ct)
    {
        var user = await userService.FindByEmailAsync(context.Email, ct);

        if (user is null || user.EmailConfirmed)
        {
            // Return success even if user not found or already confirmed (idempotent, no enumeration)
            return FluentResult.Success();
        }

        var result = await userTokenService.CreateAsync(
            user.Id,
            UserTokenType.EmailConfirmation,
            _options.EmailConfirmationTokenExpiry,
            ct: ct);

        await mediator.SendAsync(new SendEmailCommand(
            user.Email.Value,
            "Confirm your email",
            $"Your confirmation code is: {result.RawToken}"), ct);

        return FluentResult.Success();
    }

    public class Context
    {
        public required Email Email { get; set; }
    }
}
