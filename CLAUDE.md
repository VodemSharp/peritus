# Peritus — Agent Guide

This document captures the architecture, patterns, and hard-won gotchas of the Peritus codebase so future agents (and
humans) can move fast without breaking things.

## 1. Project Overview

Peritus is a .NET 10 modular monolith template with Aspire orchestration. It is **not** a microservices architecture —
it is a single deployable unit split into layers:

| Layer           | Projects                                                                                                                                                  | Responsibility                                                                                               |
|-----------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------|
| Host            | `Peritus.AppHost`, `Peritus.ServiceDefaults`                                                                                                              | Aspire orchestration, Docker Compose env, container registry config                                          |
| Apps            | `Peritus.Api`, `Peritus.Web`                                                                                                                              | Minimal API endpoints + Blazor frontend (separate deployables, same repo)                                    |
| Modules         | `Peritus.Identity`, `Peritus.Notification`                                                                                                                | Domain modules. Identity = auth/users. Notification = email/SMS                                              |
| Module Messages | `Peritus.Messages.Notification`                                                                                                                           | Inter-module command contracts                                                                               |
| Migrators       | `Peritus.Migrator`                                                                                                                                        | DbUp migration runner + admin seeding                                                                        |
| Contracts       | `Peritus.ApiContracts.Identity`, `Peritus.ApiClients`                                                                                                     | Refit interfaces + DTOs, client infrastructure + token storage                                               |
| Common          | `Peritus.Types`, `Peritus.Primitives`, `Peritus.Persistence`, `Peritus.Guard`, `Peritus.Results`, `Peritus.OpenApi`, `Peritus.Messaging`, `Peritus.Cache` | Cross-cutting primitives, EF Core helpers, JWT guard, result types, in-process mediator, distributed caching |
| Tests           | `Peritus.IntegrationTests`, `Peritus.Identity.IntegrationTests`, `Peritus.Api.IntegrationTests`                                                           | xUnit v3 integration tests                                                                                   |

**Key infrastructure:** PostgreSQL (DbUp migrations), Valkey (Redis-compatible cache for token blacklist + lockout
state), Aspire Testcontainers for integration tests.

## 2. Build System

- **Solution file:** `Peritus.slnx` (XML-based, not `.sln`)
- **Central package management:** `Directory.Packages.props` with `ManagePackageVersionsCentrally=true`
- **Warnings as errors:** `TreatWarningsAsErrors=true`
- **Build command:** `dotnet build Peritus.slnx`
- **Test command:**
  `dotnet test tests/modules/Peritus.Identity.IntegrationTests/Peritus.Identity.IntegrationTests.csproj`

## 3. The Feature Pattern

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

## 4. FluentResult Pattern

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

## 5. Strongly-Typed Value Objects

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

### ⚠️ CRITICAL: `.Value` in LINQ Queries

**NEVER access `.Value` on a value object inside an EF Core LINQ query.** The value converter handles translation when
you use the value object directly, but `.Value` breaks translation:

```csharp
// ❌ WRONG — throws InvalidOperationException at runtime
.Where(u => u.Status.Value == "Confirmed")
.Where(u => u.NormalizedEmail.Value == "foo@bar.com")

// ✅ CORRECT — EF Core translates via value converter
.Where(u => u.Status == UserSessionStatus.Confirmed)
.Where(u => u.NormalizedEmail == new Email("foo@bar.com"))
```

### ⚠️ CRITICAL: `FindAsync` with Value Objects

`DbContext.FindAsync` expects the **value object itself**, not the underlying primitive:

```csharp
// ❌ WRONG — throws ArgumentException about type mismatch
var user = await db.Users.FindAsync(userId.Value);

// ✅ CORRECT
var user = await db.Users.FindAsync(userId);
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

## 6. EF Core & NoTracking

### Global NoTracking

The `IdentityDbContext` is configured with **global NoTracking**:

```csharp
options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
```

This means every query returns **detached entities** by default.

### Updating Entities with NoTracking

When you query an entity and then modify it, you **must explicitly attach it** before `SaveChangesAsync`:

```csharp
// Query returns detached entity
var session = await db.UserSessions.FirstOrDefaultAsync(...);

session.Status = UserSessionStatus.Terminated;

// ❌ WRONG — SaveChanges does nothing because entity is not tracked
await db.SaveChangesAsync();

// ✅ CORRECT — explicitly tell EF Core to track it as Modified
db.UserSessions.Update(session);
await db.SaveChangesAsync();
```

**All `Update()` calls in the codebase (search for `db.*.Update(`):**

- `RevokeUserSessionFeature`
- `RevokeAllSessionsFeature`
- `SignOutFeature`
- `RefreshTokenFeature`
- `ConfirmEmailFeature`
- `EnableTwoFactorFeature`
- `DisableTwoFactorFeature`
- `VerifyTwoFactorSetupFeature`
- `SignInTwoFactorFeature`
- `ChangePasswordFeature`
- `ResetPasswordFeature`
- `UpdateProfileFeature`
- `UserService.UpdateAsync`

**Rule of thumb:** If you query then modify, call `.Update()`. If you call `.AddAsync()`, no `.Update()` is needed.

### Computed Columns

Some properties are database-computed and `init`-only:

```csharp
// Entity
public Email NormalizedEmail { get; init; }

// Configuration
builder.Property(e => e.NormalizedEmail)
    .HasComputedColumnSql("LOWER(\"email\")", true)  // stored computed column
    .HasMaxLength(256)
    .ValueGeneratedOnAddOrUpdate();
```

EF Core reads these on materialization but will not write them.

### Entity Configurations

`IEntityTypeConfiguration<T>` classes are **co-located with entity classes** in the same `.cs` file, not in a separate
folder. Six entities have configurations: `User`, `UserSession`, `UserRecoveryCode`, `UserExternalLogin`, `Role`,
`UserRole`.

### Interceptors

Two interceptors are registered:

- `PerformanceInterceptor` — logs slow queries (>1s by default)
- `TimestampInterceptor` — auto-sets `CreatedAt` on add and `UpdatedAt` on add/modify for entities implementing
  `ICreatedEntity` / `IUpdatedEntity`

### Snake Case Naming

PostgreSQL columns use snake_case via `EFCore.NamingConventions`:

```csharp
options.UseSnakeCaseNamingConvention();
```

C# `Email` → SQL `email`.

## 7. Endpoint Pattern

Endpoints are grouped by domain in static classes with a `Map(WebApplication)` method. Each file defines its own route
group and contains multiple private `Handle*Async` handlers:

```csharp
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
            .WithSummary("Sign in with 2FA code");
    }

    private static async Task<IResult> HandleSignInAsync(
        SignInRequest request,
        SignInFeature feature,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await feature.ExecuteAsync(new SignInFeature.Context { ... }, ct);
        return result.ToResult(x => new SignInResponse { ... });
    }
}
```

Routes are registered in `Program.cs`:

```csharp
SignInEndpoints.Map(app);
SignUpEndpoints.Map(app);
// ... etc
```

### URL Naming Conventions

All routes follow these rules:

1. **Kebab-case for multi-word segments:** `send-verification`, `verify-2fa`, `send-email-confirmation`
2. **Action-first for operations:** `POST /auth/passwords/forgot` (not `/auth/forgot-password`)
3. **Resource nesting only for true sub-resources:** `POST /accounts/sessions/{id}/revoke` (sessions are owned by the
   account)
4. **Auth vs Accounts split:**
    - `/auth/*` — unauthenticated or auth-initiating actions (signup, signin, verify-2fa during login)
    - `/auth/passwords/*` — password recovery (forgot, reset) — tag: `Passwords`
    - `/auth/emails/*` — email confirmation flow — tag: `Emails`
    - `/accounts/*` — authenticated account management (change-password, enable-2fa, sessions, confirm-phone-number)
    - `/profiles` — current user profile (standalone, plural for consistency)
5. **Terminology consistency:** use "confirm" for account-side verification, "verify" for auth-flow steps
6. **Single-word OpenAPI tags:** `Auth`, `Emails`, `Passwords`, `Sessions`, `Profile`, `Phones`, `TwoFactor`
7. **Empty-response endpoints** must declare `.Produces(StatusCodes.Status200OK)` for OpenAPI completeness

**Examples:**

```csharp
// Auth (unauthenticated)
POST /auth/signup
POST /auth/signin
POST /auth/verify-2fa              // 2FA step during sign-in
POST /auth/use-recovery-code       // recovery code sign-in
POST /auth/external/google         // Google sign-in

// Auth/Passwords (unauthenticated)
POST /auth/passwords/forgot        // tag: Passwords
POST /auth/passwords/reset         // tag: Passwords

// Auth/Emails (unauthenticated)
POST /auth/emails/send-confirmation  // tag: Emails
POST /auth/emails/confirm            // tag: Emails

// Accounts (authenticated)
POST /accounts/passwords/change    // tag: Passwords
POST /accounts/2fa/enable          // tag: TwoFactor
POST /accounts/2fa/confirm         // tag: TwoFactor
POST /accounts/2fa/disable         // tag: TwoFactor
POST /accounts/2fa/recovery-codes  // tag: TwoFactor
POST /accounts/phones/send-verification  // tag: Phones
POST /accounts/phones/verify       // tag: Phones
GET    /accounts/sessions          // tag: Sessions
POST /accounts/sessions/{id}/revoke  // tag: Sessions
POST /accounts/sessions/revoke-all // tag: Sessions

// Profiles (authenticated)
GET  /profiles                     // tag: Profile
PUT  /profiles                     // tag: Profile
```

## 8. Authentication & Authorization

- JWT Bearer auth with symmetric HMAC-SHA256 key
- Custom claims: `jti` (AccessTokenId), `UserId` (custom claim type)
- Refresh token rotation: old access token is blacklisted on every refresh
- `TokenService` returns `FluentResult<ClaimsPrincipal>` for token validation — never throws for invalid tokens
- `SessionValidationMiddleware` returns `401` with `WWW-Authenticate` header for revoked sessions
- `ClaimsPrincipal` extensions in `Peritus.Guard.Extensions.ClaimExtensions`:
    - `principal.GetUserId()` → `UserId`
    - `principal.GetAccessTokenId()` → `AccessTokenId`

## 9. Database Migrations: DbUp (Not EF Core Migrations)

**Schema is managed by DbUp, not EF Core Migrations.**

- Scripts live in `src/modules/Peritus.Identity/Persistence/Scripts/` as embedded resources
- `Peritus.Migrator` is a background worker that runs DbUp on startup, then seeds admin role/user
- EF Core is used ONLY for querying, change tracking, and `ExecuteDeleteAsync`
- The `Migrations/` folder was deleted — do NOT recreate it

**Adding schema changes:**

1. Write a new `.sql` script with `CREATE TABLE IF NOT EXISTS` / `ALTER TABLE`
2. Add it as an embedded resource in `Peritus.Identity.csproj`
3. Update the corresponding EF entity + `IEntityTypeConfiguration<T>` (co-located in the entity `.cs` file)
4. Run integration tests — they fail immediately if model and schema drift

## 10. Testing

### Test Architecture

```
InfrastructureFixture (IAsyncLifetime)
  ├─ Creates Aspire AppHost with PostgreSQL + Valkey containers
  ├─ Removes api/web resources (tests create their own WebApplicationFactory)
  ├─ Waits for migrator to finish
  ├─ Provides connection strings
  └─ Shared across all tests in a class via IClassFixture

ApiTest (IAsyncLifetime)
  ├─ Creates PeritusApplicationFactory per test method
  ├─ Injects FakeTimeProvider for deterministic time
  ├─ Provides CreateUserAsync(), SignInAsync(), CreateRestClient<T>()
  └─ Tests override ConfigureServices() and GetSettings()

IdentityApiTest : ApiTest
  └─ Adds CreateIdentityDbContext() and CreateIdentityApi()
```

### Critical Testing Gotchas

1. **Container persistence:** The PostgreSQL container uses `ContainerLifetime.Persistent`. Data accumulates across test
   runs. Use unique values (GUIDs) for any user-facing data that has uniqueness constraints.

2. **Cancellation tokens:** The fixture uses an internal `CancellationTokenSource(120s)` for container startup.
   Individual test methods typically use `TestContext.Current.CancellationToken` for API calls.

3. **Parallel execution:** xUnit v3 runs tests in parallel by default. Tests share the database. Always use unique
   emails/usernames per test.

4. **NoTracking in tests:** `CreateIdentityDbContext()` configures `NoTracking`. If you query and then want to verify
   state changes, query again after the API call — don't reuse the entity.

5. **WebApplicationFactory per test:** Each test method gets its own factory + DI container. But they all connect to the
   same persistent database.

### Creating a Test

```csharp
public class MyTests(InfrastructureFixture fixture)
    : IdentityApiTest(fixture), IClassFixture<InfrastructureFixture>
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task MyTestAsync()
    {
        var credentials = await CreateUserAsync(_ct);
        var tokens = await SignInAsync(credentials);
        var api = CreateIdentityApi(tokens.AccessToken);

        var response = await api.SomeEndpointAsync(new SomeRequest { ... }, _ct);

        ApiAssert.Success(response, result =>
        {
            Assert.Equal("expected", result.Something);
        });
    }
}
```

### Test Helpers

- `UserCredentials.Create()` generates a unique email/password pair using `Guid.CreateVersion7()`. Always use this
  instead of hardcoded values.
- `ApiAssert.SuccessAsync(...)` is available for assertions that need `await` (e.g., querying the database after an API
  call).
- `PeritusApplicationFactory` is anchored to `IApiMarker` (`src/apps/Peritus.Api/IApiMarker.cs`) for
  `WebApplicationFactory<T>` discovery.

## 11. Module Registration

Each module exposes an extension method on `WebApplicationBuilder`:

```csharp
public static class IdentityExtensions
{
    public static WebApplicationBuilder AddIdentityModule(this WebApplicationBuilder builder)
    {
        // Services
        builder.Services.AddScoped<IUserSessionService, UserSessionService>();
        builder.Services.AddScoped<ITokenService, TokenService>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IUserTokenService, UserTokenService>();
        builder.Services.AddScoped<ISessionValidator, SessionValidator>();

        // Features - Auth
        builder.Services.AddScoped<SignInFeature>();
        builder.Services.AddScoped<SignUpFeature>();
        builder.Services.AddScoped<TokenRefreshFeature>();
        builder.Services.AddScoped<SignInGoogleFeature>();
        builder.Services.AddScoped<SignInTwoFactorFeature>();
        builder.Services.AddScoped<SignInRecoveryCodeFeature>();
        builder.Services.AddScoped<EmailSendConfirmationFeature>();
        builder.Services.AddScoped<EmailConfirmFeature>();
        builder.Services.AddScoped<PasswordResetSendFeature>();
        builder.Services.AddScoped<PasswordResetFeature>();

        // Features - Accounts
        builder.Services.AddScoped<SignOutFeature>();
        builder.Services.AddScoped<PasswordChangeFeature>();
        builder.Services.AddScoped<SessionListFeature>();
        builder.Services.AddScoped<SessionRevokeFeature>();
        builder.Services.AddScoped<SessionRevokeAllFeature>();
        builder.Services.AddScoped<TwoFactorEnableFeature>();
        builder.Services.AddScoped<TwoFactorVerifySetupFeature>();
        builder.Services.AddScoped<TwoFactorDisableFeature>();
        builder.Services.AddScoped<TwoFactorGenerateRecoveryCodesFeature>();
        builder.Services.AddScoped<PhoneNumberSendVerificationFeature>();
        builder.Services.AddScoped<PhoneNumberVerifyFeature>();

        // Features - Profile
        builder.Services.AddScoped<ProfileGetFeature>();
        builder.Services.AddScoped<ProfileUpdateFeature>();

        builder.AddIdentityDbContext();
        return builder;
    }
}
```

Called from `src/apps/Peritus.Api/Program.cs`:

```csharp
builder.AddIdentityModule();
```

## 12. Inter-Module Communication

Modules must **never call each other's features directly**. Identity must not reference `Peritus.Notification` or inject
`EmailSendFeature`. Instead, modules communicate through the **in-process mediator**.

### Architecture

```
Identity Feature ──publishes──> IMediator ──dispatches──> Notification Feature
```

- **Commands** live in module-specific `.Messages` projects (e.g., `Peritus.Messages.Notification`)
- **Identity** depends only on `Peritus.Messages.Notification` (the contract), never on `Peritus.Notification` (the
  implementation)
- The **API host** wires command handlers inside each module's `Add*Module()` extension method

### Publishing a Command

```csharp
// Inside an Identity feature
public class SignUpFeature(IMediator mediator, ...)
{
    public async Task<FluentResult<Result>> ExecuteAsync(Context context, CancellationToken ct)
    {
        // ... create user ...
        await mediator.SendAsync(new SendEmailCommand(to, subject, body), ct);
        // ...
    }
}
```

### Wiring Handlers

```csharp
// Inside NotificationExtensions (the module owns its own wiring)
public static WebApplicationBuilder AddNotificationModule(this WebApplicationBuilder builder)
{
    builder.Services.AddScoped<EmailSendFeature>();
    builder.Services.AddCommandHandler<SendEmailCommand, EmailSendFeature>((feature, cmd, _) =>
    {
        feature.ExecuteAsync(new EmailSendFeature.Context { To = cmd.To, Subject = cmd.Subject, Body = cmd.Body }, CancellationToken.None);
        return Task.CompletedTask;
    });
    return builder;
}
```

### Rules

- Features do NOT implement `ICommandHandler<>` — the registration lambda maps command to `Feature.Context`
- `IMediator` is registered via `builder.Services.AddMediator()` in the API layer
- The mediator is **synchronous** — handlers run in-process and the publisher `await`s completion
- When extracting to microservices, replace the in-process mediator with a message broker (same command types)

## 13. Account Lockout

Account lockout uses Valkey (not PostgreSQL) to avoid write amplification. There is **no dedicated lockout service** —
the logic is inline within `SignInFeature` using `IDistributedCacheService` directly:

**Cache keys** (defined in `IdentityCacheKeys.cs`):

- `identity:auth:failed:{userId}` → failure count as a string
- `identity:auth:lockout:{userId}` → lockout end timestamp as Unix milliseconds

**Flow:**

1. On failed sign-in, `RecordAccessFailedAsync` increments the failure counter.
2. When count reaches `MaxFailedAccessAttempts`, `auth:lockout:{userId}` is set with a TTL of
   `DefaultLockoutTimeSpan + 1 minute`, and the failed counter is cleared.
3. On successful sign-in, `ResetAccessFailedAsync` removes both keys.
4. Uses `TimeProvider` for testability (stores timestamps in values, not just cache TTL).
5. `LockoutEnabled` on the `User` entity is checked first.

## 14. Common Pitfalls Checklist

- [ ] Did I call `db.*.Update(entity)` after querying with NoTracking and before `SaveChangesAsync`?
- [ ] Did I avoid `.Value` on value objects in LINQ queries?
- [ ] Did I pass the value object (not `.Value`) to `FindAsync`?
- [ ] Did I use `nameof(context.Property)` for all `FluentResult.ValidationProblem` field names?
- [ ] Did I use `FluentResult` instead of throwing for business errors (in features AND services)?
- [ ] Did I register new features as scoped in `IdentityExtensions`?
- [ ] Did I map new endpoints in the appropriate `*Endpoints.cs` file?
- [ ] Did I update `IIdentityApi` and DTOs in `Peritus.ApiContracts.Identity` for new endpoints?
- [ ] Did I use unique values (GUIDs) in tests that touch uniqueness constraints?
- [ ] Did I update the DbUp SQL script (not EF migrations) for schema changes?
- [ ] Did I add the new script as an embedded resource in the `.csproj`?
- [ ] Did I update the `IEntityTypeConfiguration<T>` co-located in the entity file?
- [ ] Did I add `using Peritus.Identity.Types;` when referencing module-specific types like `UserSessionStatus`?
- [ ] Did I validate phone numbers with E.164 format (`+1234567890`) before processing?
- [ ] Does the auth cookie expiry match the JWT `exp` claim?
- [ ] Did I use `using` directives (or `using` aliases for disambiguation) instead of fully qualified type names like
  `Peritus.Identity.Options.IdentityOptions`?
- [ ] Did I avoid inline magic strings? Extract them to `const` fields or `static readonly` members on the class that
  uses them.
- [ ] Did I avoid calling another module's feature directly? Use `IMediator` and a command from that module's
  `.Messages` project instead.

## 15. Logging with LoggerMessage

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

## 16. File Locations Reference

| Concern                      | Location                                                                  |
|------------------------------|---------------------------------------------------------------------------|
| Module DI registration       | `src/modules/Peritus.Identity/IdentityExtensions.cs`                      |
| EF entities + configurations | `src/modules/Peritus.Identity/Persistence/Entities/**/*.cs`               |
| DbUp SQL scripts             | `src/modules/Peritus.Identity/Persistence/Scripts/`                       |
| Features - Auth              | `src/modules/Peritus.Identity/Features/Auth/`                             |
| Features - Accounts          | `src/modules/Peritus.Identity/Features/Accounts/`                         |
| Features - Profile           | `src/modules/Peritus.Identity/Features/Profile/`                          |
| Services                     | `src/modules/Peritus.Identity/Services/`                                  |
| Service abstractions         | `src/modules/Peritus.Identity/Services/Abstractions/`                     |
| Module helpers               | `src/modules/Peritus.Identity/Helpers/`                                   |
| Module options               | `src/modules/Peritus.Identity/Options/`                                   |
| Module types/enums           | `src/modules/Peritus.Identity/Types/`                                     |
| API endpoints                | `src/apps/Peritus.Api/Endpoints/**/*.cs`                                  |
| Auth endpoints               | `src/apps/Peritus.Api/Endpoints/Auth/`                                    |
| Account endpoints            | `src/apps/Peritus.Api/Endpoints/Accounts/`                                |
| Profile endpoints            | `src/apps/Peritus.Api/Endpoints/ProfileEndpoints.cs`                      |
| Refit contracts              | `src/contracts/Peritus.ApiContracts.Identity/`                            |
| API client infrastructure    | `src/contracts/Peritus.ApiClients/`                                       |
| Value object definitions     | `src/common/Peritus.Types/`                                               |
| Value converters             | `src/common/Peritus.Persistence/ValueConverters/`                         |
| Interceptors                 | `src/common/Peritus.Persistence/Interceptors/`                            |
| Mediator abstractions        | `src/common/Peritus.Messaging/`                                           |
| Distributed caching          | `src/common/Peritus.Cache/`                                               |
| OpenAPI transformers         | `src/common/Peritus.OpenApi/`                                             |
| Aspire service defaults      | `src/common/Peritus.ServiceDefaults/`                                     |
| Module commands/events       | `src/messages/Peritus.Messages.Notification/`                             |
| Migrator                     | `src/migrators/Peritus.Migrator/`                                         |
| Web frontend                 | `src/apps/Peritus.Web/`                                                   |
| API marker interface         | `src/apps/Peritus.Api/IApiMarker.cs`                                      |
| Test fixture                 | `tests/common/Peritus.IntegrationTests/Fixtures/InfrastructureFixture.cs` |
| Test factory                 | `tests/common/Peritus.IntegrationTests/PeritusApplicationFactory.cs`      |
| Test assertions              | `tests/common/Peritus.IntegrationTests/Assertions/ApiAssert.cs`           |
| Test base classes            | `tests/common/Peritus.IntegrationTests/Abstractions/ApiTest.cs`           |
| Identity test scenarios      | `tests/modules/Peritus.Identity.IntegrationTests/Scenarios/`              |
| API health tests             | `tests/api/Peritus.Api.IntegrationTests/`                                 |
