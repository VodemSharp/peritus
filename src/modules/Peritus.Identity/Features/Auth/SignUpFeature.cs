using Microsoft.Extensions.Options;
using Peritus.FluentResults;
using Peritus.Identity.Persistence;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Messages.Notification;
using Peritus.Messaging.Abstractions;
using Peritus.Persistence.Extensions;
using Peritus.Types.Identity.Users;
using Peritus.Types.Tokens;

namespace Peritus.Identity.Features.Auth;

public class SignUpFeature(
    IUserService userService,
    IUserSessionService userSessionService,
    IUserTokenService userTokenService,
    ISessionValidator sessionValidator,
    IMediator mediator,
    IOptions<IdentityOptions> identityOptions,
    IdentityDbContext db)
{
    private readonly IdentityOptions _options = identityOptions.Value;

    public async Task<FluentResult<Result>> ExecuteAsync(Context context, CancellationToken ct)
    {
        var userExists = await userService.AnyByEmailAsync(context.Email, ct);

        if (userExists)
        {
            return FluentResult<Result>.ValidationProblem(nameof(context.Email),
                "Email address is already in use.");
        }

        return await db.ExecuteInTransactionAsync(async () =>
        {
            var userId = await userService.CreateAsync(context.Email, context.Password, ct: ct);

            // Send email confirmation
            var tokenResult = await userTokenService.CreateAsync(
                userId, UserTokenType.EmailConfirmation, _options.EmailConfirmationTokenExpiry, ct: ct);

            await mediator.SendAsync(new SendEmailCommand(
                context.Email.Value,
                "Confirm your email",
                $"Your confirmation code is: {tokenResult.RawToken}"), ct);

            if (_options.RequireConfirmedEmail)
            {
                return FluentResult<Result>.Success(new Result
                {
                    EmailConfirmationRequired = true
                });
            }

            var sessionResult = await userSessionService.CreateAsync(
                userId, context.IpAddress, context.UserAgent, ct: ct);

            await sessionValidator.SetAsync(sessionResult.AccessTokenId, sessionResult.ExpiredAt, ct);

            return FluentResult<Result>.Success(new Result
            {
                AccessToken = sessionResult.Tokens.AccessToken,
                RefreshToken = sessionResult.Tokens.RefreshToken,
                EmailConfirmationRequired = false
            });
        }, ct);
    }

    public class Context
    {
        public required Email Email { get; set; }
        public required Password Password { get; set; }
        public required IpAddress? IpAddress { get; set; }
        public required UserAgent UserAgent { get; set; }
    }

    public class Result
    {
        public AccessToken? AccessToken { get; set; }
        public RefreshToken? RefreshToken { get; set; }
        public required bool EmailConfirmationRequired { get; set; }
    }
}
