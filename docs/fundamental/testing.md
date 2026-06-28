# Peritus — Testing Guide

[← Back to CLAUDE.md](../../CLAUDE.md)

**Related:** [Architecture](architecture.md) · [Refit Client](refit.md) · [API Layer](api.md) · [Patterns](patterns.md)

One feature ↔ one scenario test file, named `{Feature}Tests.cs` under `Scenarios/{Domain}/`
(see [the vertical slice](architecture.md)). Tests drive the slice through the `IIdentityApi` Refit
client, exactly as a real consumer would. **Rare exception (manual approval):** a feature whose behaviour
differs by a template configuration flag may split into one file per configuration —
`{Feature}{Configuration}Tests.cs` (file name = class name) — each pinning its config via
`GetSettings()` (see [the slice rules](architecture.md)).

## Test Architecture

```
InfrastructureFixture (IAsyncLifetime)
  ├─ Creates Aspire AppHost with PostgreSQL + Valkey containers
  ├─ Removes the api resource (tests create their own WebApplicationFactory)
  ├─ Waits for migrator to finish
  ├─ Provides connection strings
  └─ Shared across the whole test assembly via `[assembly: AssemblyFixture(typeof(InfrastructureFixture))]`

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
        // One call sets up an authenticated user + Refit client
        var (_, _, api) = await CreateAuthenticatedUserAsync(_ct);

        var response = await api.SomeEndpointAsync(new SomeRequest { ... }, _ct);

        var result = ApiAssert.Success(response);   // returns non-null Content
        Assert.Equal("expected", result.Something);
    }
}
```

### Test Helpers

Reuse the shared flow helpers (`IdentityApiTest`) instead of re-deriving setup in each test:

- `UserCredentials.Create()` generates a unique email/password pair using `Guid.CreateVersion7()`. Always
  use this instead of hardcoded values (the PostgreSQL container is persistent — see gotcha #1).
- `CreateAuthenticatedUserAsync(ct)` → `AuthenticatedUser(Credentials, Tokens, Api)` (a `readonly record
  struct` in the test `Types/` folder). Deconstruct and discard what you don't need:
  `var (_, _, api) = await CreateAuthenticatedUserAsync(_ct);`
- `SetupTwoFactorAsync(api, ct)` (`static`) → `TwoFactorSetup(Secret, RecoveryCodes)` — enables 2FA and
  confirms it with a generated TOTP. Don't use it in tests that exercise the enable/confirm steps
  themselves.
- `TotpTestHelper.Generate(secret)` (`Helpers/`) — TOTP code for a shared secret. Use it instead of
  hand-rolling `new Totp(Base32Encoding.ToBytes(...))` per file.
- `FakeGoogleTokenValidator` (`Infrastructure/`) — inject via `ConfigureServices` to bypass Google's
  network call; set its `Payload` to a `GoogleSignInPayload` (or `null` to simulate an invalid token).
- Assign the Refit client to a variable before calling it (`var api = CreateIdentityApi(...); await
  api.X(...)`), never `await CreateIdentityApi().X(...)`.
- Multi-session tests (session list/revoke) deliberately set up sessions manually — leave them as-is.
- `PeritusApplicationFactory` is anchored to `IApiMarker` (`src/apps/Peritus.Api/IApiMarker.cs`) for
  `WebApplicationFactory<T>` discovery.

### Assertions

`ApiAssert` (`tests/common/Peritus.IntegrationTests/Assertions/ApiAssert.cs`) handles the Refit 12
null-narrowing for you (see [refit.md](refit.md)):

- `ApiAssert.Success(response)` — asserts success and **returns the non-null `Content`** (synchronous).
- `ApiAssert.ValidationErrorAsync(response, nameof(Req.Field), "msg")` — for
  `FluentResult.ValidationProblem` (`ValidationProblemDetails.Errors[field]`).
- `ApiAssert.ValidationMessageAsync(response, "msg")` — for `FluentResult.ValidationMessage` (plain
  `ProblemDetails.Detail`).
