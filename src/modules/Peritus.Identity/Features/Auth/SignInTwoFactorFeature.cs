using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Peritus.AspNetCore.Extensions;
using Peritus.FluentResults;
using Peritus.Identity.Helpers;
using Peritus.Identity.Persistence;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;
using Peritus.Persistence.Extensions;
using Peritus.Types.Identity.Users;
using Peritus.Types.Tokens;

namespace Peritus.Identity.Features.Auth;

public class SignInTwoFactorFeature(
    ITokenService tokenService,
    IUserService userService,
    IUserSessionService userSessionService,
    ISessionValidator sessionValidator,
    IdentityDbContext db)
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/verify-2fa", async (
                Request request,
                SignInTwoFactorFeature feature,
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
            .WithSummary("Sign in with two-factor authentication");
    }

    private async Task<FluentResult<Response>> ExecuteAsync(Request request, CancellationToken ct)
    {
        var tokenResult = await tokenService.ValidateTwoFactorTokenAsync(request.TwoFactorToken, ct);
        if (!tokenResult.IsSuccess)
        {
            return FluentResult<Response>.ValidationProblem(
                nameof(request.TwoFactorToken),
                IdentityErrorCodes.InvalidTwoFactorToken);
        }

        var userId = tokenResult.Result;
        var user = await userService.GetByIdAsync(userId, ct);

        if (!user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecret))
        {
            return FluentResult<Response>.ValidationProblem(
                nameof(request.Code),
                IdentityErrorCodes.TwoFactorNotEnabled);
        }

        if (!TotpHelper.ValidateCode(user.TwoFactorSecret, request.Code))
        {
            return FluentResult<Response>.ValidationProblem(
                nameof(request.Code),
                IdentityErrorCodes.InvalidTwoFactorCode);
        }

        return await db.ExecuteInTransactionAsync(async () =>
        {
            var sessionResult = await userSessionService.CreateAsync(
                user.Id, request.IpAddress, request.UserAgent, ct: ct);

            await sessionValidator.SetAsync(sessionResult.AccessTokenId, sessionResult.ExpiredAt, ct);

            return FluentResult<Response>.Success(new Response
            {
                AccessToken = sessionResult.Tokens.AccessToken,
                RefreshToken = sessionResult.Tokens.RefreshToken
            });
        }, ct);
    }

    public class Request
    {
        [JsonPropertyName("twoFactorToken")] public required string TwoFactorToken { get; set; }
        [JsonPropertyName("code")] public required string Code { get; set; }
        [JsonIgnore] public IpAddress? IpAddress { get; set; }
        [JsonIgnore] public UserAgent UserAgent { get; set; }
    }

    public class Response
    {
        [JsonPropertyName("accessToken")] public required AccessToken AccessToken { get; set; }
        [JsonPropertyName("refreshToken")] public required RefreshToken RefreshToken { get; set; }
    }
}
