# Solution structure (key map)

High-level map of the Peritus modular monolith. Excludes build/IDE artifacts.

## Top-level
- `Peritus.slnx` — solution entry.
- `Directory.Packages.props` — central package management.
- `.template.config/template.json` — `dotnet new peritus` template manifest.
- `AGENTS.md` — AI entrypoint linking core docs and guides.

## Documentation
- Cross-cutting numbered docs: `docs/01-*.md` through `docs/19-*.md` (templates, logging, OpenAPI, persistence, auth/JWT, telemetry, testing, AI prompts).
- Guides: `docs/guides/add-module.md`, `docs/guides/add-feature.md`.

## Source
- API: `src/api/Peritus.Api` — composition root, middleware, options, endpoint mappings; stays thin and delegates to modules.
- Modules: `src/modules/Peritus.Identity` (current module) with features, services, persistence, options, types; `Peritus.Identity.Migrator` seeds/migrates DB.
- Contracts: `src/contracts/Peritus.Identity.RestContracts` — DTOs + Refit clients using shared strong types.
- Shared libraries (`src/common/*`):
  - `Peritus.Primitives`, `Peritus.Types`, `Peritus.Results`
  - `Peritus.Persistence` (EF glue, interceptors, converters)
  - `Peritus.OpenApi` (security transformer), `Peritus.Logging`, `Peritus.Guard`
- Host & defaults:
  - `src/host/Peritus.AppHost` — Aspire distributed app (db, cache, migrator, API).
  - `src/host/Peritus.ServiceDefaults` — telemetry/resilience/health defaults.

## Tests
- Common test infra: `tests/common/Peritus.IntegrationTests` (Aspire test host, `PeritusApplicationFactory`, `FakeTimeProvider`).
- API tests: `tests/api/Peritus.Api.IntegrationTests`.
- Module tests: `tests/modules/Peritus.Identity.IntegrationTests`.
