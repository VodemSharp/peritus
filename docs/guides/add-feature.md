# Guide: add a new feature to a module

Use this checklist to add a vertical slice to an existing module (e.g., `Peritus.Identity`). Keep the API layer thin and defer logic to the module.

## 0) Prerequisites
- Skim architecture and cross-cutting docs: modular monolith @/docs/12-modular-monolith.md, persistence @/docs/09-persistence-efcore.md, strong IDs @/docs/03-strongly-typed-ids.md, converters @/docs/16-strong-ids-json-converters.md @/docs/17-typeconverter-strong-ids.md, results @/docs/10-result-pattern.md, OpenAPI security @/docs/08-openapi-scalar.md.
- Look at Identity’s registration to see how features are added: @/src/modules/Peritus.Identity/IdentityExtensions.cs#18-38.

## 1) Shape the contract
- Add request/response DTOs in the module’s RestContracts project (e.g., `src/contracts/Peritus.Identity.RestContracts`). Reuse strong types for IDs/values.
- Extend the Refit interface so tests and consumers get typed clients; keep routes and models aligned with API endpoints.

## 2) Model the domain
- Add value objects or IDs under `src/common/Peritus.Types/<Area>` if shared; otherwise keep module-local types.
- Update entities under `src/modules/<Module>/Persistence/Entities` and DbContext. Regenerate EF migrations if persistence changes.

## 3) Implement the feature
- Add a feature/service class under `src/modules/<Module>/Features/<Area>` that encapsulates the use case and returns `FluentResult`.
- Register the feature in `Add<Module>Module()` so DI can resolve it.@/src/modules/Peritus.Identity/IdentityExtensions.cs#18-38
- Keep side effects (emails, tokens, caching) inside the module layer; the API should only orchestrate and map results.

## 4) Expose via API endpoint
- Add a minimal API endpoint in `src/api/Peritus.Api/Endpoints/...` that:
  - Binds input DTOs, validates auth, and calls the feature.
  - Maps results to HTTP (e.g., 200/201 vs. 401/403). Avoid duplicating business rules.
- Secure with JWT/bearer when needed; OpenAPI bearer security is applied centrally.@/src/common/Peritus.OpenApi/Transformers/BearerSecurityDocumentTransformer.cs#1-120

## 5) Testing
- Add integration tests under `tests/modules/<Module>.IntegrationTests/Scenarios/...`.
- Base on `ApiTest` + `InfrastructureFixture` to run against real Aspire infra and to control time via `FakeTimeProvider`.@/tests/common/Peritus.IntegrationTests/Abstractions/ApiTest.cs#15-141
- Cover happy path, validation errors, auth/forbidden, time-sensitive/expiry paths, and persistence effects.

## 6) Documentation
- Update the module doc under `docs/modules/<module>.md` with the new feature surface, endpoints, and options.
- Add links in `AGENTS.md` so AI assistants and humans can find the new slice quickly.
