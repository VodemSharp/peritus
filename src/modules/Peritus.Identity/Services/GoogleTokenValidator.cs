using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Peritus.Identity.Services.Abstractions;
using Peritus.Identity.Types;

namespace Peritus.Identity.Services;

public class GoogleTokenValidator(
    IHttpClientFactory httpClientFactory,
    IOptions<IdentityOptions> identityOptions) : IGoogleTokenValidator
{
    private const string GoogleJwksUri = "https://www.googleapis.com/oauth2/v3/certs";
    private const string GoogleIssuer = "https://accounts.google.com";
    private const string EmailVerifiedClaim = "email_verified";

    public async Task<GoogleSignInPayload?> ValidateAsync(string idToken, CancellationToken ct = default)
    {
        try
        {
            var handler = new JsonWebTokenHandler();
            var token = handler.ReadJsonWebToken(idToken);
            var kid = token.Kid;

            if (string.IsNullOrEmpty(kid))
            {
                return null;
            }

            var httpClient = httpClientFactory.CreateClient();
            var jwksResponse = await httpClient.GetStringAsync(GoogleJwksUri, ct);
            var keySet = new JsonWebKeySet(jwksResponse);
            var signingKey = keySet.GetSigningKeys().SingleOrDefault(k => k.KeyId == kid);

            if (signingKey == null)
            {
                return null;
            }

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = GoogleIssuer,
                ValidateAudience = true,
                ValidAudience = identityOptions.Value.GoogleClientId,
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
                EmailVerified =
                    bool.TryParse(claimsIdentity.FindFirst(EmailVerifiedClaim)?.Value, out var verified) && verified,
                Name = claimsIdentity.FindFirst(JwtRegisteredClaimNames.Name)?.Value,
                Picture = claimsIdentity.FindFirst(JwtRegisteredClaimNames.Picture)?.Value
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }
}
