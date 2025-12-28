using Microsoft.AspNetCore.Identity;
using Peritus.FluentResults;
using Peritus.Identity.Persistence.Entities.Users;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Types.Identity.Users;
using Peritus.Types.Tokens;

namespace Peritus.Identity.Features.Auth;

public class SignInFeature(
    IPasswordHasher<User> passwordHasher,
    IUserSessionService userSessionService,
    IUserService userService)
{
    public async Task<FluentResult<Result>> ExecuteAsync(Context context, CancellationToken ct)
    {
        const string invalidCredentialsField = nameof(context.Password);
        const string invalidCredentialsMessage = "Invalid credentials!";

        var user = await userService.GetOrDefaultAsync(context.Email);

        if (user == null)
        {
            return FluentResult<Result>.ValidationProblem(invalidCredentialsField, invalidCredentialsMessage);
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, context.Password);

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, context.Password);
            await userService.UpdateAsync(user);
        }

        switch (result)
        {
            case PasswordVerificationResult.Failed:
                return FluentResult<Result>.ValidationProblem(invalidCredentialsField, invalidCredentialsMessage);

            case PasswordVerificationResult.SuccessRehashNeeded:
            case PasswordVerificationResult.Success:
                var tokens = await userSessionService.CreateAsync(
                    user.Id,
                    context.IpAddress,
                    context.UserAgent,
                    UserSessionProvider.Credentials,
                    ct);

                return FluentResult<Result>.Success(new Result
                {
                    AccessToken = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken
                });

            default:
                throw new ApplicationException("Unexpected result");
        }
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
