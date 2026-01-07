# Guide: add a new module

Peritus is a modular monolith. Each module owns its features, persistence, contracts, and service registration. Use Identity as the reference pattern.

## 0) Prerequisites (what to check)
- Architecture: skim the modular monolith overview to confirm module boundaries and vertical slice flow.@/docs/12-modular-monolith.md
- Persistence/IDs: note how strong IDs map through EF and JSON converters; reuse the same patterns and converters for the new module.@/docs/03-strongly-typed-ids.md @/docs/16-strong-ids-json-converters.md @/docs/17-typeconverter-strong-ids.md @/docs/09-persistence-efcore.md
- Cross-cutting: recall logging/OpenAPI defaults so your module surfaces fit existing telemetry and security expectations.@/docs/07-structured-logging-serilog.md @/docs/08-openapi-scalar.md
- Concrete example: scan Identity registration to copy service/feature/DbContext wiring shape.@/src/modules/Peritus.Identity/IdentityExtensions.cs#18-60

## 1) Create projects
- Module library: `src/modules/<Module>/<Module>.csproj` (SDK style, nullable enabled, analyzers). Copy Identity csproj as a starting point.
- Migrator (optional): `src/modules/<Module>.Migrator/<Module>.Migrator.csproj` to run migrations/seed data.
- Contracts: `src/contracts/<Module>.RestContracts/<Module>.RestContracts.csproj` for DTOs and Refit clients using shared strong types.

## 2) Define domain + persistence
- Strong types: put reusable IDs/values in `src/common/Peritus.Types/<Area>`; keep module-only types under `src/modules/<Module>/Types`.
- Entities/DbContext: add under `src/modules/<Module>/Persistence`. Apply the same DbContext configuration: split queries, `UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)`, snake_case naming, and performance/timestamp interceptors.@/src/modules/Peritus.Identity/IdentityExtensions.cs#42-57
- Migrations: point the migrator at the module DbContext and generate migrations. Ensure connection strings follow the existing keys (`ConnectionStrings:db`).

## 3) Wire service registration
- Add `builder.Add<Module>Module()` mirroring Identity to register:
  - Services and features (scoped)
  - Options binding from configuration sections
  - DbContext with interceptors and snake_case mapping
- Call `builder.Add<Module>Module()` in API `Program` after cross-cutting setup so telemetry/auth/logging are already configured.@/src/api/Peritus.Api/Program.cs#8-24

## 4) Contracts + API surface
- Define DTOs and Refit interfaces in `src/contracts/<Module>.RestContracts`, reusing strong types so API and module stay aligned.
- Map minimal API endpoints under `src/api/Peritus.Api/Endpoints/<Area>` that delegate to module features; keep API logic thin and stateless.
- If you need cross-module interaction, use explicit service interfaces; no inter-module bus exists yet.

## 5) Testing
- Add `tests/modules/<Module>.IntegrationTests` mirroring Identity. Use Aspire test host + `PeritusApplicationFactory` with `FakeTimeProvider` for deterministic time.@/tests/common/Peritus.IntegrationTests/Abstractions/ApiTest.cs#15-141
- Cover migrations boot, happy paths, auth/forbidden, and expiry flows.

## 6) Documentation
- Add `docs/modules/<module>.md` describing surface area, endpoints, and options.
- Link the new module doc from `AGENTS.md` and update `docs/20-solution-structure.md` if new projects/folders are added.
