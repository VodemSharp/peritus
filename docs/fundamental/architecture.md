# Peritus — Architecture & Vertical Slices

[← Back to AGENTS.md](../../AGENTS.md)

**Related:** [Patterns](patterns.md) · [API Layer](api.md) · [Testing](testing.md) · [Persistence](persistence.md)

This is the conceptual entry point. Read it before adding any capability.

## The Vertical Slice

Peritus is organized by **capability**, not by technical layer. Every operation is one self-contained slice, and the
slice has a **1 ↔ 1 ↔ 1 ↔ 1** shape:

```
1 contract DTO(s)   1 Refit method        1 feature class            1 test file
─────────────────   ───────────────       ─────────────────────      ────────────────
SignUpRequest   ◄──►  IIdentityApi     ◄──►  SignUpFeature        ◄──►  SignUpTests
SignUpResponse        .SignUpAsync           (ExecuteAsync +
                                              static MapEndpoint)
```

Concretely, the sign-up slice lives in exactly these files:

| Role                    | File                                                                                       |
|-------------------------|--------------------------------------------------------------------------------------------|
| Contract DTOs           | `src/contracts/Peritus.ApiContracts.Identity/Auth/SignUpRequest.cs`, `…/SignUpResponse.cs` |
| Refit method            | `src/contracts/Peritus.ApiContracts.Identity/IIdentityApi.cs` → `SignUpAsync`              |
| Feature (logic + route) | `src/modules/Peritus.Identity/Features/Auth/SignUpFeature.cs`                              |
| DI + route registration | `src/modules/Peritus.Identity/IdentityExtensions.cs`                                       |
| Test                    | `tests/modules/Peritus.Identity.IntegrationTests/Features/Auth/SignUpTests.cs`             |

**Rules of the slice:**

- **One feature = one class = one operation = one `ExecuteAsync`.** Do not add a second operation to an existing
  feature.
- **One feature ↔ one test file**, named `{Feature}Tests.cs` and placed under the matching
  `Features/{Domain}/` folder. The test drives the slice through `IIdentityApi`, exactly as a real client would.
    - **Configuration variants stay in the same file.** When a feature behaves differently under a template
      configuration flag (e.g. an `IdentityOptions` flag such as `RequireConfirmedEmail`), do **not** split into a
      second file. Pin the config declaratively with `[Settings(key, value)]` on the test class (applies to every test
      in it) or on a single `[Fact]` (applies to just that test). See [testing.md](testing.md).
- **The feature owns its endpoint.** There is no central endpoints folder; the route is declared by the feature's own
  static `MapEndpoint` (see [api.md](api.md)).
- **Contract DTOs mirror the feature's nested `Request`/`Response`.** The feature defines server-side nested `Request`/
  `Response` types (the JSON shape); the contract project mirrors them as standalone DTOs for the Refit client and
  tests. Keep the two in sync — same property names and `[JsonPropertyName]`.
- For a complete inventory of every slice currently in the system, see the generated
  [endpoints reference](../reference/endpoints.md).

## Adding a Capability — the Chain

Touch these files, in order, to add one new operation (worked example: "update user preferences"):

1. **Contract DTO (s)** — `src/contracts/Peritus.ApiContracts.Identity/Accounts/UserPreferencesUpdateRequest.cs`
   (one public type per file; only the `using`s that type needs).
2. **Refit method** — add to `IIdentityApi.cs` with the exact route:
   ```csharp
   [Put("/accounts/preferences")]
   Task<IApiResponse> UpdatePreferencesAsync([Body] UserPreferencesUpdateRequest request, CancellationToken ct = default);
   ```
3. **Feature class** — `src/modules/Peritus.Identity/Features/Accounts/UserPreferencesUpdateFeature.cs`
   with a static `MapEndpoint(IEndpointRouteBuilder)` and a private `ExecuteAsync` returning
   `FluentResult`/`FluentResult<Response>`. See [patterns.md](patterns.md) for the feature body and
   [api.md](api.md) for the endpoint shape.
4. **DI registration** — in `IdentityExtensions.AddIdentityModule`:
   `builder.Services.AddScoped<UserPreferencesUpdateFeature>();`
5. **Route registration** — in `IdentityExtensions.MapIdentityEndpoints`:
   `UserPreferencesUpdateFeature.MapEndpoint(app);`
6. **Test** —
   `tests/modules/Peritus.Identity.IntegrationTests/Features/Accounts/UserPreferencesUpdateTests.cs`, covering the
   success path and each validation failure. Reuse the shared flow helpers (see [testing.md](testing.md)).

After the slice compiles and tests pass, run **`/sync-docs`** to regenerate
[reference/endpoints.md](../reference/endpoints.md) and flag any documentation drift.

## Module Boundaries

Modules (`Peritus.Identity`, `Peritus.Notification`) never call each other's features directly. They communicate through
the in-process `IMediator` and command contracts in the `*.Messages` projects — see
[api.md → Inter-Module Communication](api.md#inter-module-communication).
