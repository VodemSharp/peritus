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

public class PasswordResetSendFeature(
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

        if (user is null)
        {
            // Return success to prevent account enumeration
            return FluentResult.Success();
        }

        var result = await userTokenService.CreateAsync(
            user.Id,
            UserTokenType.PasswordReset,
            _options.PasswordResetTokenExpiry,
            ct: ct);

        await mediator.SendAsync(new SendEmailCommand(
            user.Email.Value,
            "Reset your password",
            $"Your password reset code is: {result.RawToken}"), ct);

        return FluentResult.Success();
    }

    public class Context
    {
        public required Email Email { get; set; }
    }
}
