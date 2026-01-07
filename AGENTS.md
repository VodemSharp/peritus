## AI entrypoint to Peritus
- Overview & architecture: @/docs/12-modular-monolith.md, @/docs/20-solution-structure.md
- Cross-cutting setup: package mgmt @/docs/05-central-package-management.md, logging @/docs/07-structured-logging-serilog.md, OpenAPI @/docs/08-openapi-scalar.md, persistence @/docs/09-persistence-efcore.md, results @/docs/10-result-pattern.md, Refit clients @/docs/11-refit-clients.md, telemetry @/docs/15-telemetry-opentelemetry.md, strong ID converters @/docs/16-strong-ids-json-converters.md @/docs/17-typeconverter-strong-ids.md
- Auth/JWT: introduction @/docs/06-jwt-introduction.md, refresh blacklist @/docs/13-auth-refresh-blacklist.md
- Infrastructure & Aspire: @/docs/14-aspire-containers-infra.md
- Testing: integration + Aspire host + FakeTimeProvider @/docs/18-testing-integration.md
- Templates: dotnet new template @/docs/01-custom-templates.md
- AI authoring tips: @/docs/19-ai-prompts-best-practices.md

## Modules
- Identity module: code at `src/modules/Peritus.Identity`; migrator at `src/modules/Peritus.Identity.Migrator`; contracts at `src/contracts/Peritus.Identity.RestContracts`.
- Module design guide: @/docs/guides/add-module.md
- Feature slice guide: @/docs/guides/add-feature.md

## Host & runtime
- API composition root: `src/api/Peritus.Api` (Program wires cross-cutting concerns then `AddIdentityModule`).
- Aspire host: `src/host/Peritus.AppHost`; defaults in `src/host/Peritus.ServiceDefaults`.
- Shared libraries: `src/common/*` (guard, logging, OpenApi, persistence, primitives, results, types).

## Tests
- Common fixtures + helpers: `tests/common/Peritus.IntegrationTests`
- API tests: `tests/api/Peritus.Api.IntegrationTests`
- Module tests: `tests/modules/Peritus.Identity.IntegrationTests`

## Guides
- Adding a module: @/docs/guides/add-module.md
- Adding a feature: @/docs/guides/add-feature.md
- AI-focused docs writing: @/docs/19-ai-prompts-best-practices.md

## When adding or changing things
- Add/update docs under `docs/` or `docs/guides/` and link them here.
- Keep links pointing to code paths/snippets so agents can jump directly.
