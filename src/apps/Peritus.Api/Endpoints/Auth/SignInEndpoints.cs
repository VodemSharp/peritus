using Peritus.Api.Extensions;
using Peritus.ApiContracts.Identity.Auth;
using Peritus.Identity.Features.Auth;

namespace Peritus.Api.Endpoints.Auth;

public static class SignInEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app
            .MapGroup("/auth")
            .WithTags("Auth");

        group.MapPost("/signin", HandleSignInAsync)
            .Produces<SignInResponse>()
            .WithSummary("Sign in");

        group.MapPost("/verify-2fa", HandleTwoFactorSignInAsync)
            .Produces<TwoFactorSignInResponse>()
            .WithSummary("Sign in with two-factor authentication code");

        group.MapPost("/use-recovery-code", HandleRecoveryCodeSignInAsync)
            .Produces<RecoveryCodeSignInResponse>()
            .WithSummary("Sign in with recovery code");

        group.MapPost("/external/google", HandleGoogleSignInAsync)
            .Produces<GoogleSignInResponse>()
            .WithSummary("Google sign in");
    }

    private static async Task<IResult> HandleSignInAsync(
        SignInRequest request,
        SignInFeature feature,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await feature.ExecuteAsync(
            new SignInFeature.Context
            {
                Email = request.Email,
                Password = request.Password,
                IpAddress = httpContext.GetRemoteIpAddress(),
                UserAgent = httpContext.Request.GetUserAgent()
            }, ct);

        return result.ToResult(x =>
            new SignInResponse
            {
                AccessToken = x.AccessToken,
                RefreshToken = x.RefreshToken,
                RequiresTwoFactor = x.RequiresTwoFactor,
                TwoFactorToken = x.TwoFactorToken
            }
        );
    }

    private static async Task<IResult> HandleTwoFactorSignInAsync(
        TwoFactorSignInRequest request,
        SignInTwoFactorFeature feature,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await feature.ExecuteAsync(
            new SignInTwoFactorFeature.Context
            {
                TwoFactorToken = request.TwoFactorToken,
                Code = request.Code,
                IpAddress = httpContext.GetRemoteIpAddress(),
                UserAgent = httpContext.Request.GetUserAgent()
            }, ct);

        return result.ToResult(x =>
            new TwoFactorSignInResponse
            {
                AccessToken = x.AccessToken,
                RefreshToken = x.RefreshToken
            }
        );
    }

    private static async Task<IResult> HandleRecoveryCodeSignInAsync(
        RecoveryCodeSignInRequest request,
        SignInRecoveryCodeFeature feature,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await feature.ExecuteAsync(
            new SignInRecoveryCodeFeature.Context
            {
                TwoFactorToken = request.TwoFactorToken,
                Code = request.Code,
                IpAddress = httpContext.GetRemoteIpAddress(),
                UserAgent = httpContext.Request.GetUserAgent()
            }, ct);

        return result.ToResult(x =>
            new RecoveryCodeSignInResponse
            {
                AccessToken = x.AccessToken,
                RefreshToken = x.RefreshToken
            }
        );
    }

    private static async Task<IResult> HandleGoogleSignInAsync(
        GoogleSignInRequest request,
        SignInGoogleFeature feature,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await feature.ExecuteAsync(
            new SignInGoogleFeature.Context
            {
                IdToken = request.IdToken,
                IpAddress = httpContext.GetRemoteIpAddress(),
                UserAgent = httpContext.Request.GetUserAgent()
            }, ct);

        return result.ToResult(x =>
            new GoogleSignInResponse
            {
                AccessToken = x.AccessToken,
                RefreshToken = x.RefreshToken
            }
        );
    }
}
