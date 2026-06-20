# Peritus — Agent Guide

This document captures the architecture, patterns, and hard-won gotchas of the Peritus codebase so future agents (and
humans) can move fast without breaking things.

## Project Overview

Peritus is a .NET 10 modular monolith template with Aspire orchestration. It is **not** a microservices architecture —
it is a single deployable unit split into layers:

| Layer           | Projects                                                                                                                                                  | Responsibility                                                                                               |
|-----------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------|
| Host            | `Peritus.AppHost`, `Peritus.ServiceDefaults`                                                                                                              | Aspire orchestration, Docker Compose env, container registry config                                          |
| Apps            | `Peritus.Api`                                                                                                                                             | Minimal API endpoints                                                                                        |
| Modules         | `Peritus.Identity`, `Peritus.Notification`                                                                                                                | Domain modules. Identity = auth/users. Notification = email/SMS                                              |
| Module Messages | `Peritus.Messages.Notification`                                                                                                                           | Inter-module command contracts                                                                               |
| Migrators       | `Peritus.Migrator`                                                                                                                                        | DbUp migration runner + admin seeding                                                                        |
| Contracts       | `Peritus.ApiContracts.Identity`, `Peritus.ApiClients`                                                                                                     | Refit interfaces + DTOs, client infrastructure + token storage                                               |
| Common          | `Peritus.Types`, `Peritus.Primitives`, `Peritus.Persistence`, `Peritus.Guard`, `Peritus.Results`, `Peritus.OpenApi`, `Peritus.Messaging`, `Peritus.Cache` | Cross-cutting primitives, EF Core helpers, JWT guard, result types, in-process mediator, distributed caching |
| Tests           | `Peritus.IntegrationTests`, `Peritus.Identity.IntegrationTests`, `Peritus.Api.IntegrationTests`                                                           | xUnit v3 integration tests                                                                                   |

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

| Guide                                        | Topics                                                                           |
|----------------------------------------------|----------------------------------------------------------------------------------|
| [Patterns & Value Objects](docs/patterns.md) | Feature pattern, FluentResult, value objects (full), LoggerMessage, LINQ methods |
| [Persistence](docs/persistence.md)           | EF Core NoTracking, `.Update()` rule, computed columns, DbUp migrations          |
| [API Layer](docs/api.md)                     | Endpoint pattern, URL conventions, auth, module registration, mediator, lockout  |
| [Testing](docs/testing.md)                   | Test architecture, fixtures, gotchas, helpers                                    |

---

## Critical Rules (Quick Reference)

These are the most common mistakes. See the detailed guides above for full explanations and examples.

### FluentResult

All feature methods return `FluentResult` instead of throwing:

```csharp
return FluentResult.Success();
return FluentResult<Result>.Success(new Result { ... });
return FluentResult.ValidationProblem(nameof(context.Email), "Error message.");
return FluentResult.ValidationMessage("State error message.");
return FluentResult.NotFound("Detail message.");
return FluentResult.InternalError("Detail message.");
```

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
| Auth (unauthenticated) | `/auth/signup`, `/auth/signin`                                              |
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
- `ClaimsPrincipal` extensions in `Peritus.Guard.Extensions.ClaimExtensions`:
    - `principal.GetUserId()` → `UserId`
    - `principal.GetAccessTokenId()` → `AccessTokenId`

## Folder Organization

Place types in semantically correct folders. A `record struct` holding an HTTP client name string is a **type**, not a
service.

```csharp
// ❌ WRONG — HttpClientName is not a service
// src/common/Peritus.Types/Services/HttpClientName.cs

// ✅ CORRECT
// src/common/Peritus.Types/Http/HttpClientName.cs
```

| What it is                               | Where it goes            | Examples                                                              |
|------------------------------------------|--------------------------|-----------------------------------------------------------------------|
| Type / value object / constant container | `Types/` or project root | `HttpClientName`                                                      |
| Business logic / handler / provider      | `Services/`              | `TokenService`, `SessionValidator`                                    |
| DI registration helpers                  | `Extensions/`            | `IdentityApiClientExtensions`                                         |
| Interfaces                               | `Abstractions/`          | `ITokenStorage`                                                       |

## Common Pitfalls Checklist

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
- [ ] Did I use `using` directives (or `using` aliases for disambiguation) instead of fully qualified type names like
  `Peritus.Identity.Options.IdentityOptions`?
- [ ] Did I avoid inline magic strings? Extract them to `const` fields or `static readonly` members on the class that
  uses them.
- [ ] Did I avoid calling another module's feature directly? Use `IMediator` and a command from that module's
  `.Messages` project instead.
- [ ] Did I use `response.Error.Content` (not `?.`) after `!response.IsSuccessful`, and skip `response.Content is null`
  checks? `ApiResponse<T>` has `MemberNotNullWhen` annotations — trust them.

### Refit ApiResponse Null Checks

Refit's `ApiResponse<T>` has `MemberNotNullWhen` annotations on `IsSuccessful`:

```csharp
// ❌ WRONG — redundant null checks
if (!response.IsSuccessful || response.Content is null)
{
    _errorMessage = response.Error?.Content; // ?. is unnecessary
    return;
}

// ✅ CORRECT — IsSuccessful tells the compiler
if (!response.IsSuccessful)
{
    _errorMessage = response.Error.Content; // Error is non-null here
    return;
}

// Content is non-null here
var token = response.Content.AccessToken;
```

| `IsSuccessful` | `Content` | `Error`  |
|----------------|-----------|----------|
| `true`         | non-null  | null     |
| `false`        | null      | non-null |

## File Locations Reference

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
| API marker interface         | `src/apps/Peritus.Api/IApiMarker.cs`                                      |
| Test fixture                 | `tests/common/Peritus.IntegrationTests/Fixtures/InfrastructureFixture.cs` |
| Test factory                 | `tests/common/Peritus.IntegrationTests/PeritusApplicationFactory.cs`      |
| Test assertions              | `tests/common/Peritus.IntegrationTests/Assertions/ApiAssert.cs`           |
| Test base classes            | `tests/common/Peritus.IntegrationTests/Abstractions/ApiTest.cs`           |
| Identity test scenarios      | `tests/modules/Peritus.Identity.IntegrationTests/Scenarios/`              |
| API health tests             | `tests/api/Peritus.Api.IntegrationTests/`                                 |
