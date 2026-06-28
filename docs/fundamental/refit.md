# Peritus — Refit Client & `ApiResponse` Guide

[← Back to CLAUDE.md](../../CLAUDE.md)

**Related:** [API Layer](api.md) · [Testing](testing.md) · [Patterns](patterns.md)

The typed HTTP client is the Refit interface `IIdentityApi`
(`src/contracts/Peritus.ApiContracts.Identity/IIdentityApi.cs`). Integration tests call the API
exclusively through it (`CreateIdentityApi(...)`), so the null-narrowing rules below apply to every
test assertion as well as to any production consumer of `IApiClients`.

## `ApiResponse` Null Checks (Refit 12+)

**Refit 12 removed the shadowed members on `IApiResponse<T>`** (`IsSuccessful`, `Error`,
`ContentHeaders`, `IsSuccessStatusCode`). Consequences:

- **`IsSuccessful` no longer narrows `Content` to non-null.** To get a non-null `Content`, guard on
  **`IsSuccessfulWithContent`** instead (`HasContent` is the standalone check):

```csharp
// ❌ WRONG (Refit 12) — IsSuccessful no longer narrows Content
if (!response.IsSuccessful) { ... }
var token = response.Content.AccessToken; // CS8602

// ✅ CORRECT
if (!response.IsSuccessfulWithContent) { ... }
var token = response.Content.AccessToken; // Content is non-null here
```

- **`HasResponseError(out var ex)` yields a *nullable* `ex`.** The compiler can't see through
  `Assert.True(...)` or a `?:`, so assert it (`Assert.NotNull(ex)`) or use `?.` before dereferencing:

```csharp
Assert.True(response.HasResponseError(out var ex), response.Error?.Message);
Assert.NotNull(ex);                                  // narrows ex for the next line
Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
```

- `response.Error?.Message` and `response.Content` still compile — they bind to the inherited base
  members.

| `IsSuccessfulWithContent` | `Content` |
|---------------------------|-----------|
| `true`                    | non-null  |
| `false`                   | null      |

This is exactly how the shared test assertions are written — see `ApiAssert`
(`tests/common/Peritus.IntegrationTests/Assertions/ApiAssert.cs`) and `ApiTest.SignInAsync`
(`tests/common/Peritus.IntegrationTests/Abstractions/ApiTest.cs`).

## STJ Behavior Change

Refit 12's default `System.Text.Json` serializer now reads numbers from JSON strings
(`NumberHandling = AllowReadingFromString`). Opt back out with `JsonNumberHandling.Strict` on your
`JsonSerializerOptions` if you need the old strictness.
