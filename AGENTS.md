# Peritus — Agent Guide

This document captures the architecture, patterns, and hard-won gotchas of the Peritus codebase so future agents (and
humans) can move fast without breaking things.

## Project Overview

Peritus is a .NET 10 modular monolith template with Aspire orchestration. It is **not** a microservices architecture —
it is a single deployable unit split into layers:

| Layer           | Projects                                                                                                                                                                        | Responsibility                                                                                                                                 |
|-----------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------|
| Host            | `Peritus.AppHost`, `Peritus.ServiceDefaults`                                                                                                                                    | Aspire orchestration, Docker Compose env, container registry config                                                                            |
| Apps            | `Peritus.Api`                                                                                                                                                                   | Minimal API endpoints                                                                                                                          |
| Modules         | `Peritus.Identity`, `Peritus.Notification`                                                                                                                                      | Domain modules. Identity = auth/users. Notification = email/SMS                                                                                |
| Module Messages | `Peritus.Messages.Notification`                                                                                                                                                 | Inter-module command contracts                                                                                                                 |
| Migrators       | `Peritus.Migrator`                                                                                                                                                              | DbUp migration runner + admin seeding                                                                                                          |
| Contracts       | `Peritus.ApiContracts.Identity`, `Peritus.ApiClients`                                                                                                                           | Refit interfaces + DTOs, client infrastructure + token storage                                                                                 |
| Common          | `Peritus.Types`, `Peritus.Primitives`, `Peritus.Persistence`, `Peritus.Guard`, `Peritus.Results`, `Peritus.AspNetCore`, `Peritus.OpenApi`, `Peritus.Messaging`, `Peritus.Cache` | Cross-cutting primitives, EF Core helpers, JWT guard, result types, ASP.NET Core result/HTTP helpers, in-process mediator, distributed caching |
| Tests           | `Peritus.IntegrationTests`, `Peritus.Identity.IntegrationTests`, `Peritus.Api.IntegrationTests`                                                                                 | xUnit v3 integration tests                                                                                                                     |

**Key infrastructure:** PostgreSQL (DbUp migrations), Valkey (Redis-compatible cache for token blacklist + lockout
state), Aspire Testcontainers for integration tests.

## Build System

- **Solution file:** `Peritus.slnx` (XML-based, not `.sln`)
- **Central package management:** `Directory.Packages.props` with `ManagePackageVersionsCentrally=true`
- **Warnings as errors:** `TreatWarningsAsErrors=true`
- **Build command:** `dotnet build Peritus.slnx`
- **Test command:**
  `dotnet test tests/modules/Peritus.Identity.IntegrationTests/Peritus.Identity.IntegrationTests.csproj`

---

## Detailed Guides

**Fundamental** (hand-curated principles — read these):

| Guide                                                    | Topics                                                                                |
|----------------------------------------------------------|---------------------------------------------------------------------------------------|
| [Architecture](docs/fundamental/architecture.md)         | Vertical slice (1 contract ↔ 1 feature ↔ 1 endpoint ↔ 1 test), add-a-capability chain |
| [Patterns & Value Objects](docs/fundamental/patterns.md) | Feature pattern, FluentResult, value objects (full), LoggerMessage, LINQ methods      |
| [Persistence](docs/fundamental/persistence.md)           | EF Core NoTracking, `.Update()` rule, computed columns, DbUp migrations               |
| [API Layer](docs/fundamental/api.md)                     | Endpoint pattern (`MapEndpoint`), URL conventions, auth, mediator, lockout            |
| [Refit Client](docs/fundamental/refit.md)                | `IIdentityApi`, `ApiResponse` null-narrowing (Refit 12+), STJ behavior                |
| [Testing](docs/fundamental/testing.md)                   | Test architecture, fixtures, helpers, assertions                                      |

**Living** (generated — regenerated by `/sync-docs`, do not hand-edit):

| Reference                                         | Contents                                      |
|---------------------------------------------------|-----------------------------------------------|
| [Endpoint Inventory](docs/reference/endpoints.md) | Every route → tag → feature → contract → test |

See [Documentation System](#documentation-system) for how these stay in sync.

---

## Critical Rules (Quick Reference)

These are the most common mistakes. See the detailed guides above for full explanations and examples.

### FluentResult

All feature methods return `FluentResult` instead of throwing. **Every failure carries an `ErrorCode`** — a
`readonly record struct (string Code, string Message)` pairing a SCREAMING_SNAKE_CASE code with its canonical message —
so the client localizes off the code. Pass the catalog entry; the message comes with it:

```csharp
return FluentResult.Success();
return FluentResult<Result>.Success(new Result { ... });
return FluentResult.ValidationProblem(nameof(context.Password), IdentityErrorCodes.InvalidCredentials);
return FluentResult.ValidationMessage(IdentityErrorCodes.TwoFactorNotEnabled);
return FluentResult.NotFound(IdentityErrorCodes.SessionNotFound);
return FluentResult.InternalError(ErrorCodes.Internal, exception);
return FluentResult.ValidationProblem(nameof(context.Password),
    IdentityErrorCodes.AccountLocked, remainingMinutes); // dynamic args → {0} template + wire `args`
```

Codes live in `Peritus.FluentResults.ErrorCodes` (generic) and `IdentityErrorCodes` (domain), each a `static class` of
`static readonly ErrorCode` fields. Field validation (400) returns top-level `code: "VALIDATION_ERROR"` plus an
`errors:[{field, code, message}]` array; state/NotFound/Internal return a top-level `code` + `detail`. For a dynamic
message, write the canonical text as a positional `{0}` template and pass `params object?[] args`; the server renders an
English fallback and ships the values as a wire `args` array for the client to localize. `InternalError` is logged
centrally at Error level — pass the `Exception` when you have one. See
[patterns.md → Error Codes](docs/fundamental/patterns.md#error-codes).

**Do NOT throw exceptions for business validation.** Use `FluentResult.ValidationProblem`. This applies to services too.

### Value Objects in LINQ Queries

**NEVER access `.Value` on a value object inside an EF Core LINQ query.**

```csharp
// ❌ WRONG — throws InvalidOperationException at runtime
.Where(u => u.Status.Value == "Confirmed")

// ✅ CORRECT — EF Core translates via value converter
.Where(u => u.Status == UserSessionStatus.Confirmed)
```

### FindAsync with Value Objects

`DbContext.FindAsync` expects the **value object itself**, not the underlying primitive:

```csharp
// ❌ WRONG — throws ArgumentException about type mismatch
var user = await db.Users.FindAsync(userId.Value);

// ✅ CORRECT
var user = await db.Users.FindAsync(userId);
```

### NoTracking + Update

The `IdentityDbContext` uses **global NoTracking**. If you query then modify, call `.Update()`:

```csharp
var session = await db.UserSessions.FirstOrDefaultAsync(...);
session.Status = UserSessionStatus.Terminated;

// ❌ WRONG — SaveChanges does nothing
db.SaveChangesAsync();

// ✅ CORRECT
db.UserSessions.Update(session);
await db.SaveChangesAsync();
```

### URL Naming Conventions

| Rule                   | Example                                                                     |
|------------------------|-----------------------------------------------------------------------------|
| Kebab-case multi-word  | `send-verification`, `verify-2fa`                                           |
| Action-first           | `POST /auth/passwords/forgot`                                               |
| Sub-resource nesting   | `POST /accounts/sessions/{id}/revoke`                                       |
| Auth (unauthenticated) | `/auth/signup`, `/auth/signin`, `/auth/signin/google`                       |
| Passwords              | `/auth/passwords/forgot`, `/accounts/passwords/change`                      |
| Emails                 | `/auth/emails/send-confirmation`                                            |
| Accounts (auth'd)      | `/accounts/2fa/enable`, `/accounts/sessions`                                |
| Profile (auth'd)       | `GET /profiles`, `PUT /profiles`                                            |
| Tags (single word)     | `Auth`, `Passwords`, `Emails`, `Sessions`, `Profile`, `Phones`, `TwoFactor` |

## Authentication & Authorization

- JWT Bearer auth with symmetric HMAC-SHA256 key
- Custom claims: `jti` (AccessTokenId), `UserId` (custom claim type)
- Refresh token rotation: old access token is blacklisted on every refresh
- `TokenService` returns `FluentResult<ClaimsPrincipal>` for token validation — never throws for invalid tokens
- `SessionValidationMiddleware` returns `401` with `WWW-Authenticate` header for revoked sessions
- **External sign-in (Google)** at `POST /auth/signin/google`: `IGoogleTokenValidator` (impl
  `GoogleTokenValidator`) validates the Google ID token against Google's JWKS. Audience is **always**
  validated against `IdentityOptions.GoogleClientId` — **fail-closed**: if it is unconfigured (`required`, see below),
  tokens are rejected, never silently accepted. The validator's catch excludes
  `OperationCanceledException` so cancellation propagates instead of looking like an invalid token. Tests inject a
  `FakeGoogleTokenValidator` (return a `GoogleSignInPayload` or `null`) to skip the network.
- `ClaimsPrincipal` extensions in `Peritus.Guard.Extensions.ClaimExtensions`:
    - `principal.GetUserId()` → `UserId`
    - `principal.GetAccessTokenId()` → `AccessTokenId`

## Module Options

- **One options class per module.** `IdentityOptions` holds *all* Identity configuration; do not add per-feature options
  classes (there is no separate `GoogleAuthOptions`). Bound from the `IdentityOptions`
  config section.
- **Secrets that gate security are `required`.** `GoogleClientId` is `public required string` so a missing value fails
  at startup binding rather than silently disabling audience validation. No code constructs
  `new IdentityOptions()`, so `required` breaks nothing.

## Folder Organization

Place types in semantically correct folders. A `record struct` holding an HTTP client name string is a **type**, not a
service.

```csharp
// ❌ WRONG — HttpClientName is not a service
// src/common/Peritus.Types/Services/HttpClientName.cs

// ✅ CORRECT
// src/common/Peritus.Types/Http/HttpClientName.cs
```

| What it is                               | Where it goes            | Examples                           |
|------------------------------------------|--------------------------|------------------------------------|
| Type / value object / constant container | `Types/` or project root | `HttpClientName`                   |
| Business logic / handler / provider      | `Services/`              | `TokenService`, `SessionValidator` |
| DI registration helpers                  | `Extensions/`            | `IdentityApiClientExtensions`      |
| Interfaces                               | `Abstractions/`          | `ITokenStorage`                    |

**One public type per file, named after the type.** Do not bundle related DTOs into a shared `*Contracts.cs`. In
`Peritus.ApiContracts.Identity`, each request/response lives in its own file under a feature folder
(`Auth/SignInRequest.cs`, `Auth/SignInResponse.cs`, `Accounts/PasswordChangeRequest.cs`, …) and carries **only the
`using`s its own type needs** (e.g. token-only responses do not import user value-object namespaces).

## Common Pitfalls Checklist

- [ ] Did I call `db.*.Update(entity)` after querying with NoTracking and before `SaveChangesAsync`?
- [ ] Did I avoid `.Value` on value objects in LINQ queries?
- [ ] Did I pass the value object (not `.Value`) to `FindAsync`?
- [ ] Did I use `nameof(context.Property)` for all `FluentResult.ValidationProblem` field names?
- [ ] Did I pass an `ErrorCode` catalog entry (from `ErrorCodes` or `IdentityErrorCodes`, never an inline literal) to
  every `ValidationProblem` / `ValidationMessage` / `NotFound` / `InternalError`? And pass the `Exception` to
  `InternalError`? (For a dynamic message, use a `{0}` template + `params object?[] args`, not a baked-in string.)
- [ ] Did I use `FluentResult` instead of throwing for business errors (in features AND services)?
- [ ] Did I register new features as scoped in `IdentityExtensions`?
- [ ] Did I add a static `MapEndpoint()` to the feature and call it in `IdentityExtensions.MapIdentityEndpoints()`?
- [ ] Did I update `IIdentityApi` and DTOs in `Peritus.ApiContracts.Identity` for new endpoints?
- [ ] Did I use unique values (GUIDs) in tests that touch uniqueness constraints?
- [ ] Did I update the DbUp SQL script (not EF migrations) for schema changes?
- [ ] Did I add the new script as an embedded resource in the `.csproj`?
- [ ] Did I update the `IEntityTypeConfiguration<T>` co-located in the entity file?
- [ ] Did I add `using Peritus.Identity.Types;` when referencing module-specific types like `UserSessionStatus`?
- [ ] Did I validate phone numbers with E.164 format (`+1234567890`) before processing?
- [ ] Did I use `using` directives (or `using` aliases for disambiguation) instead of fully qualified type names like
  `Peritus.Identity.Options.IdentityOptions`?
- [ ] Did I avoid inline magic strings? Extract them to `const` fields or `static readonly` members on the class that
  uses them.
- [ ] Did I avoid `/// <summary>` XML doc comments and explanatory inline comments? Code is self-documenting here;
  conventions live in these docs, not inline.
- [ ] Did I avoid tuples (`(string, int)`, `ValueTuple`) in signatures and returns? Declare a named type (`record` /
  `readonly record struct`) instead so members have meaningful names.
- [ ] Did I avoid calling another module's feature directly? Use `IMediator` and a command from that module's
  `.Messages` project instead.
- [ ] Did I guard on `IsSuccessfulWithContent` (not `IsSuccessful`) when I need `Content` non-null, and
  `Assert.NotNull` / `?.` the `HasResponseError` out-var before dereferencing it?
  See [Refit Client](docs/fundamental/refit.md).

## Documentation System

The docs are a cross-linked "AI database" in two tiers:

- **Fundamental** — hand-curated *principles and conventions*: this file plus the `docs/fundamental/*.md`
  guides (architecture, patterns, persistence, api, refit, testing). Edit these by hand when a convention actually
  changes. They describe *how we build*, not *what currently exists*.
- **Living** — a mechanically-derivable *inventory* under `docs/reference/` (currently
  [endpoints.md](docs/reference/endpoints.md)). **Generated, never hand-edited.** It carries an
  `AUTO-GENERATED` banner and is regenerated by the `/sync-docs` command.

**Cross-link convention:** every `docs/fundamental/*.md` opens with `[← Back to AGENTS.md](../../AGENTS.md)`
and a `**Related:** …` line; this file's [Detailed Guides](#detailed-guides) table links them all.

**Keeping it current — `/sync-docs` (manual only):** after you add or change endpoints/features/tests **and the change
is approved**, run `/sync-docs`. It regenerates the living inventory and audits the fundamental docs for path/name
drift, reporting discrepancies for a human to resolve (it never silently rewrites principles). It is **never** run
automatically — decisions churn before approval, so syncing is a deliberate post-approval step.

## File Locations Reference

| Concern                      | Location                                                                                         |
|------------------------------|--------------------------------------------------------------------------------------------------|
| Module DI registration       | `src/modules/Peritus.Identity/IdentityExtensions.cs`                                             |
| EF entities + configurations | `src/modules/Peritus.Identity/Persistence/Entities/**/*.cs`                                      |
| DbUp SQL scripts             | `src/modules/Peritus.Identity/Persistence/Scripts/`                                              |
| Features - Auth              | `src/modules/Peritus.Identity/Features/Auth/`                                                    |
| Features - Accounts          | `src/modules/Peritus.Identity/Features/Accounts/`                                                |
| Features - Profile           | `src/modules/Peritus.Identity/Features/Profile/`                                                 |
| Services                     | `src/modules/Peritus.Identity/Services/`                                                         |
| Google token validator       | `src/modules/Peritus.Identity/Services/GoogleTokenValidator.cs`                                  |
| Service abstractions         | `src/modules/Peritus.Identity/Services/Abstractions/`                                            |
| Module helpers               | `src/modules/Peritus.Identity/Helpers/`                                                          |
| Module options               | `src/modules/Peritus.Identity/Options/`                                                          |
| Module types/enums           | `src/modules/Peritus.Identity/Types/`                                                            |
| Endpoint mapping             | static `MapEndpoint()` on each feature class (`Features/**/*Feature.cs`)                         |
| Route registration           | `IdentityExtensions.MapIdentityEndpoints()` (one `Feature.MapEndpoint(app)` per line)            |
| Refit contracts              | `src/contracts/Peritus.ApiContracts.Identity/` (one type per file)                               |
| API client infrastructure    | `src/contracts/Peritus.ApiClients/`                                                              |
| Value object definitions     | `src/common/Peritus.Types/`                                                                      |
| Value converters             | `src/common/Peritus.Persistence/ValueConverters/`                                                |
| Interceptors                 | `src/common/Peritus.Persistence/Interceptors/`                                                   |
| Mediator abstractions        | `src/common/Peritus.Messaging/`                                                                  |
| Distributed caching          | `src/common/Peritus.Cache/`                                                                      |
| OpenAPI transformers         | `src/common/Peritus.OpenApi/`                                                                    |
| Aspire service defaults      | `src/common/Peritus.ServiceDefaults/`                                                            |
| Module commands/events       | `src/messages/Peritus.Messages.Notification/`                                                    |
| Migrator                     | `src/migrators/Peritus.Migrator/`                                                                |
| API marker interface         | `src/apps/Peritus.Api/IApiMarker.cs`                                                             |
| Test fixture                 | `tests/common/Peritus.IntegrationTests/Fixtures/InfrastructureFixture.cs`                        |
| Test factory                 | `tests/common/Peritus.IntegrationTests/PeritusApplicationFactory.cs`                             |
| Test assertions              | `tests/common/Peritus.IntegrationTests/Assertions/ApiAssert.cs`                                  |
| Test base classes            | `tests/common/Peritus.IntegrationTests/Abstractions/ApiTest.cs`                                  |
| Identity test base/helpers   | `tests/modules/Peritus.Identity.IntegrationTests/Abstractions/IdentityApiTest.cs`                |
| TOTP test helper             | `tests/modules/Peritus.Identity.IntegrationTests/Helpers/TotpTestHelper.cs`                      |
| Fake Google validator        | `tests/modules/Peritus.Identity.IntegrationTests/Infrastructure/FakeGoogleTokenValidator.cs`     |
| Test result types            | `tests/modules/Peritus.Identity.IntegrationTests/Types/` (`AuthenticatedUser`, `TwoFactorSetup`) |
| Identity test features       | `tests/modules/Peritus.Identity.IntegrationTests/Features/`                                      |
| API health tests             | `tests/api/Peritus.Api.IntegrationTests/`                                                        |
