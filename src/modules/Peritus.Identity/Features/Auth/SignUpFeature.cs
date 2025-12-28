using Peritus.FluentResults;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Types.Identity.Users;
using Peritus.Types.Tokens;

namespace Peritus.Identity.Features.Auth;

public class SignUpFeature(IUserService userService, IUserSessionService userSessionService)
{
    public async Task<FluentResult<Result>> ExecuteAsync(Context context, CancellationToken ct)
    {
        var userExists = await userService.AnyAsync(context.Email);

        if (userExists)
        {
            return FluentResult<Result>.ValidationProblem(nameof(context.Email),
                "Email address is already in use.");
        }

        var userId = await userService.CreateAsync(context.Email, context.Password);

        var tokens = await userSessionService.CreateAsync(
            userId,
            context.IpAddress,
            context.UserAgent,
            UserSessionProvider.Credentials,
            ct);

        return FluentResult<Result>.Success(new Result
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken
        });
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
        public required AccessToken AccessToken { get; set; }
        public required RefreshToken RefreshToken { get; set; }
    }
}
