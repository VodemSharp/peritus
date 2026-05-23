using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
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
    IHttpClientFactory httpClientFactory,
    IUserService userService,
    IUserSessionService userSessionService,
    ISessionValidator sessionValidator,
    IdentityDbContext db)
{
    public async Task<FluentResult<Result>> ExecuteAsync(Context context, CancellationToken ct)
    {
        var payload = await ValidateGoogleTokenAsync(context.IdToken, ct);

        if (payload is null || string.IsNullOrEmpty(payload.Email) || !payload.EmailVerified)
        {
            return FluentResult<Result>.ValidationProblem(nameof(context.IdToken),
                "Invalid or unverified Google token.");
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
                        new Email(payload.Email),
                        new Password(Guid.NewGuid().ToString()),
                        culture: null,
                        ct: ct);

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
                    user.Id,
                    context.IpAddress,
                    context.UserAgent,
                    externalLoginId,
                    ct);

                await sessionValidator.SetAsync(sessionResult.AccessTokenId, sessionResult.ExpiredAt, ct);

                return FluentResult<Result>.Success(new Result
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
            user.Id,
            context.IpAddress,
            context.UserAgent,
            externalLoginId,
            ct);

        await sessionValidator.SetAsync(existingSessionResult.AccessTokenId, existingSessionResult.ExpiredAt, ct);

        return FluentResult<Result>.Success(new Result
        {
            AccessToken = existingSessionResult.Tokens.AccessToken,
            RefreshToken = existingSessionResult.Tokens.RefreshToken
        });
    }

    private async Task<GoogleSignInPayload?> ValidateGoogleTokenAsync(string idToken, CancellationToken ct = default)
    {
        var handler = new JsonWebTokenHandler();
        var token = handler.ReadJsonWebToken(idToken);
        var kid = token.Kid;

        if (string.IsNullOrEmpty(kid))
        {
            return null;
        }

        var httpClient = httpClientFactory.CreateClient();
        var jwksResponse = await httpClient.GetStringAsync("https://www.googleapis.com/oauth2/v3/certs", ct);
        var keySet = new JsonWebKeySet(jwksResponse);
        var signingKey = keySet.GetSigningKeys().FirstOrDefault(k => k.KeyId == kid);

        if (signingKey == null)
        {
            return null;
        }

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://accounts.google.com",
            ValidateAudience = false,
            ValidateLifetime = true,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        var result = await handler.ValidateTokenAsync(idToken, validationParameters);

        if (!result.IsValid)
        {
            return null;
        }

        var claimsIdentity = result.ClaimsIdentity;

        return new GoogleSignInPayload
        {
            Sub = claimsIdentity.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? string.Empty,
            Email = claimsIdentity.FindFirst(JwtRegisteredClaimNames.Email)?.Value ?? string.Empty,
            EmailVerified = bool.TryParse(claimsIdentity.FindFirst("email_verified")?.Value, out var verified) &&
                            verified,
            Name = claimsIdentity.FindFirst(JwtRegisteredClaimNames.Name)?.Value,
            Picture = claimsIdentity.FindFirst(JwtRegisteredClaimNames.Picture)?.Value
        };
    }

    private class GoogleSignInPayload
    {
        public required string Sub { get; set; }
        public required string Email { get; set; }
        public required bool EmailVerified { get; set; }
        public string? Name { get; set; }
        public string? Picture { get; set; }
    }

    public class Context
    {
        public required string IdToken { get; set; }
        public required IpAddress? IpAddress { get; set; }
        public required UserAgent UserAgent { get; set; }
    }

    public class Result
    {
        public required AccessToken AccessToken { get; set; }
        public required RefreshToken RefreshToken { get; set; }
    }
}
