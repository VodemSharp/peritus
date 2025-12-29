# JWT introduction and validation

Peritus uses JWT bearer tokens for API authentication, with revocation support.

## Access token options
- Config-bound options hold key, issuer, audience, expiry.
  ```csharp
  public class AccessTokenOptions
  {
      public JwtKey Key { get; set; }
      public JwtIssuer Issuer { get; set; }
      public JwtAudience Audience { get; set; }
      public int ExpirySeconds { get; set; }
      public TimeSpan Expiry => TimeSpan.FromSeconds(ExpirySeconds);
  }
  ```
  @/src/common/Peritus.Guard/Options/AccessTokenOptions.cs#5-13

## API configuration (JWT bearer)
- `ConfigureAuth` wires authentication/authorization and registers helpers.
  ```csharp
  builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
      .AddJwtBearer(options =>
      {
          options.TokenValidationParameters = new TokenValidationParameters
          {
              ValidateIssuer = true,
              ValidateAudience = true,
              ValidateLifetime = true,
              ValidateIssuerSigningKey = true,
              ValidIssuer = accessTokenOptions.Issuer,
              ValidAudience = accessTokenOptions.Audience,
              IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(accessTokenOptions.Key)),
              ClockSkew = TimeSpan.Zero
          };

          options.Events = new JwtBearerEvents
          {
              OnMessageReceived = context =>
              {
                  var accessToken = context.Request.Query["access_token"];
                  var path = context.HttpContext.Request.Path;
                  if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub"))
                  {
                      context.Token = accessToken;
                  }
                  return Task.CompletedTask;
              }
          };
      });
  ```
  @/src/api/Peritus.Api/Extensions/Setup/AuthExtensions.cs#27-66
- Lifetime validator is overridden to use `TimeProvider`, honoring `ClockSkew` and enabling time-based tests.@/src/api/Peritus.Api/Extensions/Setup/AuthExtensions.cs#67-88

## Middleware pipeline
- `UseAuthentication`/`UseAuthorization` plus JWT blacklist middleware ensure revoked tokens are blocked.
  ```csharp
  app.UseForwardedHeaders()
     .UseMiddleware<CultureMiddleware>()
     .UseAuthentication()
     .UseAuthorization()
     .UseMiddleware<JwtBlacklistMiddleware>();
  ```
  @/src/api/Peritus.Api/Extensions/Setup/MiddlewareExtensions.cs#18-22
- Blacklist middleware checks `ITokenRevoker` for the current token’s `jti` and returns 401 if revoked.
  @/src/api/Peritus.Api/Middlewares/JwtBlacklistMiddleware.cs#7-29

## Token issuance
- `TokenService` builds access/refresh tokens with claims, roles, issuer, audience, expiry, and HMAC-SHA256 signing.
  ```csharp
  var tokenDescriptor = new SecurityTokenDescriptor
  {
      Subject = new ClaimsIdentity(jwtClaims),
      Issuer = _accessTokenOptions.Issuer,
      Audience = _accessTokenOptions.Audience,
      NotBefore = timeProvider.GetUtcNow().UtcDateTime,
      Expires = timeProvider.GetUtcNow().UtcDateTime.Add(_accessTokenOptions.Expiry),
      SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256)
  };
  var tokenHandler = new JsonWebTokenHandler();
  return new AccessToken(tokenHandler.CreateToken(tokenDescriptor));
  ```
  @/src/modules/Peritus.Identity/Services/TokenService.cs#73-95
- Expired tokens can be validated without lifetime checks for refresh flows using `GetPrincipalFromExpiredTokenAsync`.
  @/src/modules/Peritus.Identity/Services/TokenService.cs#25-52

## Revocation (blacklist technique)
- `TokenRevoker` (registered in `ConfigureAuth`) persists revoked token IDs; middleware rejects if revoked.

## Usage notes
- Keep `ClockSkew = 0` to avoid delayed revocation.
- For SignalR, tokens can arrive via `access_token` query; `OnMessageReceived` handles that.
- All auth flows use strong typed IDs (`AccessTokenId`, `UserId`) in claims helpers.
