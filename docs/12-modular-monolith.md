# Modular monolith architecture & vertical slices

Peritus is a modular monolith: the API is thin and delegates to modules that own features, persistence, and contracts.

## High-level structure
- API composition root: `src/api/Peritus.Api` wires middleware, auth, OpenAPI, and maps endpoints.
- Modules: `src/modules/<Module>` register services/features and DbContexts via `Add<Module>Module` (currently `Peritus.Identity`).
- Shared libs: `src/common/*` provide primitives (strong types, results), persistence glue, auth helpers, logging, OpenAPI transformer.
- Contracts: `src/contracts/<Module>.RestContracts` define DTOs and Refit clients using the same strong types.
- Host: `src/host/Peritus.AppHost` (Aspire) provisions infra (Postgres, Valkey) and projects; `Peritus.ServiceDefaults` adds telemetry/resilience/health.
- Tests: `tests/*` run end-to-end via Aspire testing host + `WebApplicationFactory` overrides.
@/docs/overview.md#3-38

## API wiring
- Program sets up cross-cutting concerns then modules:
  ```csharp
  builder
      .ConfigureAccessors()
      .ConfigureAuth()
      .ConfigureCache()
      .ConfigureDatabase()
      .ConfigureLogging()
      .ConfigureOpenApi()
      .ConfigureOptions()
      .ConfigureTime();
  builder.AddIdentityModule();
  ```
  @/src/api/Peritus.Api/Program.cs#8-24
- Middleware pipeline keeps API thin: forwarded headers, culture, authz, JWT blacklist.@/src/api/Peritus.Api/Extensions/Setup/MiddlewareExtensions.cs#18-22

## Module registration (Identity)
- Services, features, options, DbContext, interceptors, and snake_case are registered together:
  ```csharp
  builder.Services.AddScoped<IUserSessionService, UserSessionService>();
  builder.Services.AddScoped<RefreshTokenFeature>();
  builder.Services.Configure<RefreshTokenOptions>(builder.Configuration.GetSection("RefreshTokenOptions"));
  builder.AddIdentityDbContext();
  ```
  @/src/modules/Peritus.Identity/IdentityExtensions.cs#18-38
- DbContext uses Npgsql, split queries, no-tracking, interceptors (perf/timestamps), and snake_case.@/src/modules/Peritus.Identity/IdentityExtensions.cs#42-57

## Vertical slices
- Features (e.g., `SignInFeature`, `SignUpFeature`, `GetCurrentUserFeature`) are registered directly and consumed by endpoints; the API layer does not hold business logic.

## Design-by-contract mindset
- Strongly typed IDs/values flow through contracts, services, and persistence; conversions are handled centrally.
- Results are expressed via `FluentResult` to avoid exception control flow.

## Extending with new modules
- Add a new module under `src/modules/<Module>` with its own DbContext, types, features.
- Provide REST contracts under `src/contracts/<Module>.RestContracts` using the shared strong types.
- Expose `Add<Module>Module(builder)` to register services/DbContext; map endpoints in the API project to call the module’s features.
