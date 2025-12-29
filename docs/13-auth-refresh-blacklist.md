# Auth flows: refresh tokens and blacklist revocation

Peritus issues JWT access tokens and long-lived refresh tokens, stores sessions, and revokes access tokens via a blacklist middleware.

## Refresh token settings
- Refresh token expiry is configurable via options:
  ```csharp
  public class RefreshTokenOptions
  {
      public int ExpirySeconds { get; set; }
  }
  ```
  @/src/modules/Peritus.Identity/Options/RefreshTokenOptions.cs#3-6

## Session creation and refresh
- `UserSessionService.CreateAsync` issues a new access token ID, generates token pair, persists session with expiry, IP/user-agent, and provider:
  ```csharp
  var accessTokenId = AccessTokenId.Create();
  var authTokens = await tokenService.GenerateTokensAsync(userId, accessTokenId, ct);
  var utcNow = timeProvider.GetUtcNow().UtcDateTime;

  await db.UserSessions.AddAsync(new UserSession
  {
      UserId = userId,
      IpAddress = ip,
      UserAgent = userAgent,
      AccessTokenId = accessTokenId,
      RefreshToken = authTokens.RefreshToken,
      Status = UserSessionStatus.Confirmed,
      Provider = tokensProvider,
      ExpiredAt = utcNow.AddSeconds(_refreshTokenOptions.ExpirySeconds)
  }, ct);

  await db.SaveChangesAsync(ct);
  return authTokens;
  ```
  @/src/modules/Peritus.Identity/Services/UserSessionService.cs#21-44

## Access token validation and re-issuance
- `TokenService.GetPrincipalFromExpiredTokenAsync` validates issuer/audience/signing key without lifetime checks, enabling refresh flows.
  @/src/modules/Peritus.Identity/Services/TokenService.cs#25-52
- `TokenService.GenerateTokensAsync` builds JWT with `jti` (access token ID) and `userId` claim, signing with HMAC-SHA256 and configured expiry.
  @/src/modules/Peritus.Identity/Services/TokenService.cs#54-95

## Blacklist revocation
- The middleware rejects revoked tokens by checking `ITokenRevoker` for the current `jti`:
  ```csharp
  var isAuthenticated = context.User.Identity?.IsAuthenticated ?? false;
  if (!isAuthenticated) { await next(context); return; }

  var accessTokenId = context.User.GetAccessTokenId();
  var isTokenRevoked = await tokenRevoker.IsRevokedAsync(accessTokenId);
  if (!isTokenRevoked) { await next(context); return; }

  context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
  ```
  @/src/api/Peritus.Api/Middlewares/JwtBlacklistMiddleware.cs#7-29
- Revoker contract:
  ```csharp
  public interface ITokenRevoker
  {
      Task<bool> IsRevokedAsync(AccessTokenId tokenId, CancellationToken ct = default);
      Task RevokeAsync(AccessTokenId tokenId, TimeSpan expiration, CancellationToken ct = default);
  }
  ```
  @/src/common/Peritus.Guard/Services/Abstractions/ITokenRevoker.cs#5-9

## Pipeline integration
- Auth is configured with `AddJwtBearer` validation parameters; middleware order applies authentication, authorization, then blacklist check.
  @/src/api/Peritus.Api/Extensions/Setup/AuthExtensions.cs#27-66 @/src/api/Peritus.Api/Extensions/Setup/MiddlewareExtensions.cs#18-22

## Notes
- Access tokens use zero clock skew and TimeProvider-aware lifetime validator, making revocation/time-based tests deterministic.
- Refresh token expiry is independent from access token expiry; sessions store the refresh token and access token ID for revocation.
