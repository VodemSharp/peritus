# Peritus — Patterns & Value Objects

[← Back to main guide](../CLAUDE.md)

## The Feature Pattern

Every business operation is a **Feature class** living in the module:

```csharp
public class SignInFeature(
    IPasswordHasher<User> passwordHasher,
    IUserSessionService userSessionService,
    ...)
{
    public async Task<FluentResult<Result>> ExecuteAsync(Context context, CancellationToken ct)
    {
        // ... logic ...
        return FluentResult<Result>.Success(new Result { ... });
    }

    public class Context
    {
        public required Email Email { get; set; }
        public required Password Password { get; set; }
        // ...
    }

    public class Result
    {
        public required AccessToken AccessToken { get; set; }
        public required RefreshToken RefreshToken { get; set; }
    }
}
```

**Rules:**

- Feature classes are registered as **scoped** in DI (`builder.Services.AddScoped<SignInFeature>()`)
- They return `FluentResult` or `FluentResult<TResult>` — never throw for business errors
- The API layer (endpoint) maps request DTOs to `Feature.Context` and calls `feature.ExecuteAsync()`
- Features live in `src/modules/{Module}/Features/{Domain}/` (e.g., `Features/Auth/`, `Features/Accounts/`,
  `Features/Profile/`)
- Endpoints live in `src/apps/Peritus.Api/Endpoints/{Domain}/`

## FluentResult Pattern

All feature methods return `FluentResult` instead of throwing:

```csharp
// Success
return FluentResult.Success();
return FluentResult<Result>.Success(new Result { ... });

// Validation errors (maps to 400 BadRequest with ProblemDetails)
// Use nameof(context.Property) — never hardcode field names
return FluentResult.ValidationProblem(nameof(context.Email), "Error message.");
return FluentResult.ValidationProblem(new Dictionary<string, string[]> { ... });

// General validation message (no specific field — maps to ProblemDetails.Detail)
// Use this for state errors (e.g. "two-factor authentication is not enabled")
return FluentResult.ValidationMessage("Error message.");

// Not found (maps to 404)
return FluentResult.NotFound("Detail message.");

// Internal error (maps to 500)
return FluentResult.InternalError("Detail message.");
```

Endpoints convert results via `result.ToResult()` or `result.ToResult(responseMap)` in `FluentResultExtensions`.

**Critical rule:** Do NOT throw exceptions for business validation. Use `FluentResult.ValidationProblem`. This applies
to services too — `TokenService` returns `FluentResult<ClaimsPrincipal>` instead of throwing `SecurityTokenException`.

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
