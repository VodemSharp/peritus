using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Peritus.AspNetCore.Extensions;
using Peritus.FluentResults;
using Peritus.Identity.Persistence;
using Peritus.Identity.Persistence.Entities.Users;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Persistence.Extensions;
using Peritus.Types.Identity.Users;
using Peritus.Types.Tokens;

namespace Peritus.Identity.Features.Auth;

public class SignInGoogleFeature(
    IGoogleTokenValidator googleTokenValidator,
    IUserService userService,
    IUserSessionService userSessionService,
    ISessionValidator sessionValidator,
    IdentityDbContext db)
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/signin/google", async (
                Request request,
                SignInGoogleFeature feature,
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
            .WithSummary("Sign in with Google");
    }

    private async Task<FluentResult<Response>> ExecuteAsync(Request request, CancellationToken ct)
    {
        var payload = await googleTokenValidator.ValidateAsync(request.IdToken, ct);

        if (payload is null || string.IsNullOrEmpty(payload.Email) || !payload.EmailVerified)
        {
            return FluentResult<Response>.ValidationProblem(
                nameof(request.IdToken),
                IdentityErrorCodes.InvalidGoogleToken);
        }

        var externalLogin = await db.UserExternalLogins
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.Provider == ExternalLoginProvider.Google && x.ProviderKey == payload.Sub, ct);

        User user;
        UserExternalLoginId? externalLoginId;

        if (externalLogin == null)
        {
            return await db.ExecuteInTransactionAsync(async () =>
            {
                var existingUser = await userService.FindByEmailAsync(new Email(payload.Email), ct);

                if (existingUser != null)
                {
                    user = existingUser;
                }
                else
                {
                    var userId = await userService.CreateAsync(
                        new Email(payload.Email), new Password(Guid.NewGuid().ToString()), culture: null, ct: ct);

                    user = await userService.GetByIdAsync(userId, ct);
                    user.EmailConfirmed = true;
                    await userService.UpdateAsync(user, ct);
                }

                var newExternalLogin = new UserExternalLogin
                {
                    UserId = user.Id,
                    Provider = ExternalLoginProvider.Google,
                    ProviderKey = payload.Sub,
                    ProviderDisplayName = payload.Name
                };
                await db.UserExternalLogins.AddAsync(newExternalLogin, ct);
                await db.SaveChangesAsync(ct);

                externalLoginId = newExternalLogin.Id;

                var sessionResult = await userSessionService.CreateAsync(
                    user.Id, request.IpAddress, request.UserAgent, externalLoginId, ct);

                await sessionValidator.SetAsync(sessionResult.AccessTokenId, sessionResult.ExpiredAt, ct);

                return FluentResult<Response>.Success(new Response
                {
                    AccessToken = sessionResult.Tokens.AccessToken,
                    RefreshToken = sessionResult.Tokens.RefreshToken
                });
            }, ct);
        }

        user = externalLogin.User;
        externalLoginId = externalLogin.Id;

        // Existing external login — just create a session
        var existingSessionResult = await userSessionService.CreateAsync(
            user.Id, request.IpAddress, request.UserAgent, externalLoginId, ct);

        await sessionValidator.SetAsync(existingSessionResult.AccessTokenId, existingSessionResult.ExpiredAt, ct);

        return FluentResult<Response>.Success(new Response
        {
            AccessToken = existingSessionResult.Tokens.AccessToken,
            RefreshToken = existingSessionResult.Tokens.RefreshToken
        });
    }

    public class Request
    {
        [JsonPropertyName("idToken")] public required string IdToken { get; set; }
        [JsonIgnore] public IpAddress? IpAddress { get; set; }
        [JsonIgnore] public UserAgent UserAgent { get; set; }
    }

    public class Response
    {
        [JsonPropertyName("accessToken")] public required AccessToken AccessToken { get; set; }
        [JsonPropertyName("refreshToken")] public required RefreshToken RefreshToken { get; set; }
    }
}
