using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Peritus.AspNetCore.Extensions;
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

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/signup", async (
                Request request,
                SignUpFeature feature,
                HttpContext httpContext,
                CancellationToken ct) =>
            {
                request.IpAddress = httpContext.GetRemoteIpAddress();
                request.UserAgent = httpContext.Request.GetUserAgent();

                var result = await feature.ExecuteAsync(request, ct);
                return result.ToResult();
            })
            .Produces<Response>()
            .WithTags("Auth")
            .WithSummary("Sign up");
    }

    private async Task<FluentResult<Response>> ExecuteAsync(Request request, CancellationToken ct)
    {
        var userExists = await userService.AnyByEmailAsync(request.Email, ct);

        if (userExists)
        {
            return FluentResult<Response>.ValidationProblem(nameof(request.Email),
                "Email address is already in use.");
        }

        return await db.ExecuteInTransactionAsync(async () =>
        {
            var userId = await userService.CreateAsync(request.Email, request.Password, ct: ct);

            // Send email confirmation
            var tokenResult = await userTokenService.CreateAsync(
                userId, UserTokenType.EmailConfirmation, _options.EmailConfirmationTokenExpiry, ct: ct);

            await mediator.SendAsync(new SendEmailCommand(
                request.Email.Value,
                "Confirm your email",
                $"Your confirmation code is: {tokenResult.RawToken}"), ct);

            if (_options.RequireConfirmedEmail)
            {
                return FluentResult<Response>.Success(new Response
                {
                    EmailConfirmationRequired = true
                });
            }

            var sessionResult = await userSessionService.CreateAsync(
                userId, request.IpAddress, request.UserAgent, ct: ct);

            await sessionValidator.SetAsync(sessionResult.AccessTokenId, sessionResult.ExpiredAt, ct);

            return FluentResult<Response>.Success(new Response
            {
                AccessToken = sessionResult.Tokens.AccessToken,
                RefreshToken = sessionResult.Tokens.RefreshToken,
                EmailConfirmationRequired = false
            });
        }, ct);
    }

    public class Request
    {
        [JsonPropertyName("email")] public required Email Email { get; set; }
        [JsonPropertyName("password")] public required Password Password { get; set; }
        [JsonIgnore] public IpAddress? IpAddress { get; set; }
        [JsonIgnore] public UserAgent UserAgent { get; set; }
    }

    public class Response
    {
        [JsonPropertyName("accessToken")] public AccessToken? AccessToken { get; set; }
        [JsonPropertyName("refreshToken")] public RefreshToken? RefreshToken { get; set; }

        [JsonPropertyName("emailConfirmationRequired")]
        public required bool EmailConfirmationRequired { get; set; }
    }
}
