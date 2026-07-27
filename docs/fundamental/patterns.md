# Peritus — Patterns & Value Objects

[← Back to AGENTS.md](../../AGENTS.md)

**Related:
** [Architecture](architecture.md) · [API Layer](api.md) · [Persistence](persistence.md) · [Testing](testing.md)

## The Feature Pattern

Every business operation is a **Feature class** living in the module. The class holds three things: the logic
(`ExecuteAsync`), a static `MapEndpoint` that declares its route (see [api.md](api.md)), and nested
`Request`/`Response` types:

```csharp
public class SignInFeature(
    IPasswordHasher<User> passwordHasher,
    IUserSessionService userSessionService,
    ...)
{
    public static void MapEndpoint(IEndpointRouteBuilder app) { /* app.MapPost(...) → ExecuteAsync */ }

    private async Task<FluentResult<Response>> ExecuteAsync(Request request, CancellationToken ct)
    {
        // ... logic ...
        return FluentResult<Response>.Success(new Response { ... });
    }

    public class Request
    {
        public required Email Email { get; set; }
        public required Password Password { get; set; }
        // [JsonIgnore] for fields filled from HttpContext / claims, e.g. IpAddress, UserId
    }

    public class Response
    {
        public AccessToken? AccessToken { get; set; }
        public RefreshToken? RefreshToken { get; set; }
    }
}
```

**Rules:**

- Feature classes are registered as **scoped** in DI (`builder.Services.AddScoped<SignInFeature>()`)
- They return `FluentResult` or `FluentResult<Response>` — never throw for business errors
- **The feature owns its endpoint** via static `MapEndpoint`; the handler binds the nested `Request` and calls
  `feature.ExecuteAsync(...)`. There is no separate endpoints folder — see
  [api.md](api.md) and [architecture.md](architecture.md).
- Features live in `src/modules/{Module}/Features/{Domain}/` (e.g., `Features/Auth/`, `Features/Accounts/`,
  `Features/Profile/`)

## FluentResult Pattern

All feature methods return `FluentResult` instead of throwing. **Every failure carries an `ErrorCode`** — a
`readonly record struct (string Code, string Message)` pairing a machine-readable SCREAMING_SNAKE_CASE code with its
canonical English message, so the client localizes off the code — see [Error Codes](#error-codes) below.

```csharp
// Success
return FluentResult.Success();
return FluentResult<Result>.Success(new Result { ... });

// Field validation error (maps to 400 BadRequest ProblemDetails with an errors[] array)
// Use nameof(context.Property) for the field — never hardcode field names
return FluentResult.ValidationProblem(nameof(context.Password), IdentityErrorCodes.InvalidCredentials);

// Multi-field validation (one ValidationError per field — the ErrorCode supplies code + message)
return FluentResult.ValidationProblem(new[]
{
    new ValidationError(nameof(context.Email), IdentityErrorCodes.EmailNotConfirmed),
    new ValidationError(nameof(context.Password), IdentityErrorCodes.InvalidCredentials)
});

// State message (no specific field — maps to top-level code + ProblemDetails.Detail)
// Use this for state errors (e.g. "two-factor authentication is not enabled")
return FluentResult.ValidationMessage(IdentityErrorCodes.TwoFactorNotEnabled);

// Not found (maps to 404, top-level code + detail)
return FluentResult.NotFound(IdentityErrorCodes.SessionNotFound);

// Internal error (maps to 500, top-level code + detail; logged centrally — pass the exception when you have one)
return FluentResult.InternalError(ErrorCodes.Internal, exception);

// Dynamic message — pass positional args; the code's message is a {0} template the client localizes
return FluentResult.ValidationProblem(nameof(context.Password),
    IdentityErrorCodes.AccountLocked, remainingMinutes);
```

Endpoints convert results via `result.ToResult()` or `result.ToResult(responseMap)` in `FluentResultExtensions`.

**Critical rule:** Do NOT throw exceptions for business validation. Use `FluentResult.ValidationProblem`. This applies
to services too — `TokenService` returns `FluentResult<ClaimsPrincipal>` instead of throwing `SecurityTokenException`.

### Error Codes

Every failure result requires an `ErrorCode` — a `readonly record struct (string Code, string Message)` defined in
`src/common/Peritus.Results/ErrorCode.cs`. Each entry pairs a stable SCREAMING_SNAKE_CASE code with its canonical
English message; the backend never localizes — it returns the code (plus the message as a dev/fallback) and the client
maps the code to localized copy. Codes are a **stable API contract**: change values deliberately. **No magic strings** —
always reference a catalog entry, never inline a literal. For a dynamic message (e.g. a remaining-lockout duration),
write the canonical message as a positional `{0}` template (`"Try again in {0} minutes."`) and pass the values as
`params object?[] args` to the factory. The server renders the English fallback via `string.Format`; the same values
also ship on the wire as an `args` array so the client interpolates and pluralizes them itself off the stable code —
never localize on the backend.

The catalog is split to respect the module ↔ contracts boundary (Identity does not reference the contracts project).
Each catalog is a `static class` of `static readonly ErrorCode` fields:

| Catalog                                                      | Location                                                   | Examples                                                                                                         |
|--------------------------------------------------------------|------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------|
| Generic / cross-cutting (`Peritus.FluentResults.ErrorCodes`) | `src/common/Peritus.Results/ErrorCodes.cs`                 | `VALIDATION_ERROR`, `NOT_FOUND`, `INTERNAL_ERROR`                                                                |
| Identity domain (`IdentityErrorCodes`)                       | `src/modules/Peritus.Identity/Types/IdentityErrorCodes.cs` | `INVALID_CREDENTIALS`, `ACCOUNT_LOCKED`, `EMAIL_NOT_CONFIRMED`, `SESSION_NOT_FOUND`, `TWO_FACTOR_NOT_ENABLED`, … |

`ValidationProblem(field, code)` always sets the **top-level** code to `VALIDATION_ERROR`; the per-field code you pass
lands inside `errors[]`.

#### Response shapes

Field validation (400) — top-level `code: "VALIDATION_ERROR"` plus a per-field `errors[]` array:

```json
{
  "type": "...rfc9110#section-15.5.1", "title": "One or more validation errors occurred.",
  "status": 400, "code": "VALIDATION_ERROR",
  "errors": [
    { "field": "email",    "code": "EMAIL_NOT_CONFIRMED", "message": "Email not confirmed." },
    { "field": "password", "code": "INVALID_CREDENTIALS",  "message": "Invalid credentials." }
  ]
}
```

State message / NotFound / Internal — top-level `code` + `detail`, no `errors[]`:

```json
{ "status": 404, "code": "SESSION_NOT_FOUND", "detail": "Session not found." }
```

A dynamic message carries an `args` array (top-level for state/NotFound, or inside the `errors[]` entry for a field
error) — the rendered `detail`/`message` is an English fallback; the client re-renders from `code` + `args`:

```json
{
  "status": 400, "code": "VALIDATION_ERROR",
  "errors": [
    { "field": "password", "code": "ACCOUNT_LOCKED",
      "message": "Account locked due to multiple failed attempts. Try again in 5 minutes.",
      "args": ["5"] }
  ]
}
```

`code`, `errors`, and `args` ride inside the RFC 9457 ProblemDetails body as extension members
(`Results.Problem(..., extensions: { ["code"] = …, ["errors"] = … })`), so clients read them straight off the
ProblemDetails JSON.

#### Central internal-error logging

`InternalError(...)` is special: its `ToResult` conversion returns an `InternalErrorHttpResult`
(`src/common/Peritus.AspNetCore/HttpResults/`) whose `ExecuteAsync` resolves an `ILoggerFactory` from the request
services, **logs at Error level** (code, request method, path, detail, and the `Exception` if one was passed), then
writes the 500 ProblemDetails. This logs *every* internal error centrally with zero endpoint churn — always pass the
originating `Exception` to `InternalError(...)` when you have one.

## Strongly-Typed Value Objects

The project uses `readonly record struct` value objects everywhere instead of naked primitives.

### Definition

```csharp
public readonly record struct UserId(Guid Value) : IGuidValue
{
    public static implicit operator Guid(UserId userId) => userId.Value;
}

public readonly record struct Email(string Value) : IStringValue
{
    public static implicit operator string(Email email) => email.Value;
}
```

**Interfaces:**

- `IGuidValue` — for Guid-backed types (`UserId`, `RoleId`, `AccessTokenId`, etc.)
- `IStringValue` — for string-backed types (`Email`, `Password`, `UserAgent`, `RoleName`, etc.)

**Converters (automatic):**

- `GuidValueConverter<T>` — EF Core value converter for `IGuidValue`
- `StringValueConverter<T>` — EF Core value converter for `IStringValue`
- `GuidValueJsonConverter<T>` / `StringValueJsonConverter<T>` — System.Text.Json converters
- `GuidValueTypeConverter<T>` / `StringValueTypeConverter<T>` — `System.ComponentModel` type converters

They are wired in `IdentityDbContext.ConfigureConventions`:

```csharp
configurationBuilder.ConfigureGuidValue<UserId>();
configurationBuilder.ConfigureStringValue<Email>();
```

### Value Object Types

| Type                    | Backing  | Namespace                      |
|-------------------------|----------|--------------------------------|
| `UserId`                | `Guid`   | `Peritus.Types.Identity.Users` |
| `RoleId`                | `Guid`   | `Peritus.Types.Identity.Roles` |
| `AccessTokenId`         | `Guid`   | `Peritus.Types.Tokens`         |
| `UserSessionId`         | `Guid`   | `Peritus.Identity.Types`       |
| `UserTokenId`           | `Guid`   | `Peritus.Identity.Types`       |
| `Email`                 | `string` | `Peritus.Types.Identity.Users` |
| `Password`              | `string` | `Peritus.Types.Identity.Users` |
| `PhoneNumber`           | `string` | `Peritus.Types.Identity.Users` |
| `UserAgent`             | `string` | `Peritus.Types.Identity.Users` |
| `IpAddress`             | `string` | `Peritus.Types.Identity.Users` |
| `RefreshToken`          | `string` | `Peritus.Types.Tokens`         |
| `AccessToken`           | `string` | `Peritus.Types.Tokens`         |
| `SecureToken`           | `string` | `Peritus.Types.Tokens`         |
| `RoleName`              | `string` | `Peritus.Types.Identity.Roles` |
| `UserSessionStatus`     | `string` | `Peritus.Identity.Types`       |
| `UserSessionProvider`   | `string` | `Peritus.Identity.Types`       |
| `UserTokenType`         | `string` | `Peritus.Identity.Types`       |
| `ExternalLoginProvider` | `string` | `Peritus.Identity.Types`       |
| `Culture`               | `string` | `Peritus.Types.Localization`   |
| `CultureCode`           | `string` | `Peritus.Types.Localization`   |
| `CultureName`           | `string` | `Peritus.Types.Localization`   |
| `HttpClientName`        | `string` | `Peritus.Types.Http`           |
| `JwtKey`                | `string` | `Peritus.Types.Jwt`            |
| `JwtIssuer`             | `string` | `Peritus.Types.Jwt`            |
| `JwtAudience`           | `string` | `Peritus.Types.Jwt`            |

### Strongly-Typed Value Objects Over Primitives

When a value object exists for a concept, use it in method signatures instead of the underlying primitive. This
preserves type safety across the codebase.

```csharp
// Good — Email is a value object
public static string GenerateQrCodeUri(Email email, string secret)

// Bad — loses type safety
public static string GenerateQrCodeUri(string email, string secret)
```

Value objects implement implicit conversions to their underlying primitives, so they work seamlessly with external APIs
that expect strings or Guids.

### LINQ Query Methods

Use `SingleOrDefaultAsync` when the query is filtered by a unique identifier or composite key and you expect at most one
result. Use `FirstOrDefaultAsync` only when multiple matches are possible and any one is acceptable.

```csharp
// Good — session ID is unique, duplicates indicate data corruption
var session = await db.UserSessions
    .Where(x => x.Id == sessionId)
    .SingleOrDefaultAsync(ct);

// Bad — silently accepts duplicates
var session = await db.UserSessions
    .Where(x => x.Id == sessionId)
    .FirstOrDefaultAsync(ct);
```

## Logging with LoggerMessage

Use `[LoggerMessage]` source generators for all logging. Declare the logger in the primary constructor; the generator
accesses it automatically.

```csharp
public partial class MyFeature(ILogger<MyFeature> logger, ...)
{
    public async Task ExecuteAsync(...)
    {
        LogSomethingHappened(context.Id);
    }

    [LoggerMessage(LogLevel.Warning, "Something happened: {Id}")]
    private partial void LogSomethingHappened(Guid id);
}
```

**Rules:**

- Use **instance** `partial` methods (`private partial void`), not `static`
- Do **not** pass `ILogger<T>` to the partial method — the generator wires it from the primary constructor
- Use `LogLevel.Warning` for guard/unexpected cases (e.g., entity not found when it must exist)
- Use `LogLevel.Information` for normal flow events
- Use `LogLevel.Error` for data corruption / invariant violations

### Timestamp Interceptor

`TimestampInterceptor` auto-sets `CreatedAt` and `UpdatedAt` via `SaveChangesInterceptor`. For it to work, entities must
implement `ICreatedEntity` and/or `IUpdatedEntity`:

```csharp
public class UserRecoveryCode : ICreatedEntity
{
    public DateTime CreatedAt { get; set; }
    // ... other properties
}
```

**Do not manually set `CreatedAt` or `UpdatedAt`** in feature code when the entity implements the interface. The
interceptor handles it. If an entity has timestamp properties but doesn't implement the interfaces, the properties are
dead weight — either add the interface or remove the properties.
