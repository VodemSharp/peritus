# Peritus — Testing Guide

[← Back to AGENTS.md](../../AGENTS.md)

**Related:** [Architecture](architecture.md) · [Refit Client](refit.md) · [API Layer](api.md) · [Patterns](patterns.md)

One feature ↔ one test file, named `{Feature}Tests.cs` under `Features/{Domain}/`
(see [the vertical slice](architecture.md)). Tests drive the slice through the `IIdentityApi` Refit client, exactly as a
real consumer would. A feature whose behaviour differs by a template configuration flag does **not** split into a second
file — it pins the flag declaratively with `[Settings(key, value)]`
on the class or the individual `[Fact]` (see [Configuration via `[Settings]`](#configuration-via-settings)).

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
  └─ Tests override ConfigureServices() and declare config via [Settings(key, value)]

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
    : IdentityApiTest(fixture)
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

The `InfrastructureFixture` is shared across the whole assembly via
`[assembly: AssemblyFixture(typeof(InfrastructureFixture))]` (in `AssemblyFixtures.cs`); the class receives it by
constructor — no `IClassFixture`.

### Configuration via `[Settings]`

To run a test (or a whole test class) against a non-default template configuration, apply
`[Settings(key, value)]` — the key/value is fed to the `PeritusApplicationFactory` via `UseSetting`. There is no
`GetSettings()` override; attributes replace it.

- **Class level** — every test in the class gets the setting:
  ```csharp
  [Settings("IdentityOptions:MaxFailedAccessAttempts", "3")]
  [Settings("IdentityOptions:DefaultLockoutTimeSpan", "00:05:00")]
  public class SignInTests(InfrastructureFixture fixture) : IdentityApiTest(fixture) { ... }
  ```
- **Method level** — only that `[Fact]` gets the setting (use this to keep a configuration variant in the same file as
  the feature's other tests):
  ```csharp
  [Fact]
  [Settings("IdentityOptions:RequireConfirmedEmail", "true")]
  public async Task SignUp_WhenEmailConfirmationRequired_ReturnsNoTokensAsync() { ... }
  ```

Merge order is framework defaults → class attributes → method attributes; the most specific wins. Keys are plain strings
(config paths), so a typo silently no-ops — copy the exact `IdentityOptions:*` path.

### Test Helpers

Reuse the shared flow helpers (`IdentityApiTest`) instead of re-deriving setup in each test:

- `UserCredentials.Create()` generates a unique email/password pair using `Guid.CreateVersion7()`. Always use this
  instead of hardcoded values (the PostgreSQL container is persistent — see gotcha #1).
- `CreateAuthenticatedUserAsync(ct)` → `AuthenticatedUser(Credentials, Tokens, Api)` (a `readonly record
  struct` in the test `Types/` folder). Deconstruct and discard what you don't need:
  `var (_, _, api) = await CreateAuthenticatedUserAsync(_ct);`
- `SetupTwoFactorAsync(api, ct)` (`static`) → `TwoFactorSetup(Secret, RecoveryCodes)` — enables 2FA and confirms it with
  a generated TOTP. Don't use it in tests that exercise the enable/confirm steps themselves.
- `TotpTestHelper.Generate(secret)` (`Helpers/`) — TOTP code for a shared secret. Use it instead of hand-rolling
  `new Totp(Base32Encoding.ToBytes(...))` per file.
- `FakeGoogleTokenValidator` (`Infrastructure/`) — inject via `ConfigureServices` to bypass Google's network call; set
  its `Payload` to a `GoogleSignInPayload` (or `null` to simulate an invalid token).
- Assign the Refit client to a variable before calling it (`var api = CreateIdentityApi(...); await
  api.X(...)`), never `await CreateIdentityApi().X(...)`.
- Multi-session tests (session list/revoke) deliberately set up sessions manually — leave them as-is.
- `PeritusApplicationFactory` is anchored to `IApiMarker` (`src/apps/Peritus.Api/IApiMarker.cs`) for
  `WebApplicationFactory<T>` discovery.

### Assertions

`ApiAssert` (`tests/common/Peritus.IntegrationTests/Assertions/ApiAssert.cs`) handles the Refit 12 null-narrowing for
you (see [refit.md](refit.md)):

- `ApiAssert.Success(response)` — asserts success and **returns the non-null `Content`** (synchronous).
- `ApiAssert.ValidationErrorAsync(response, nameof(Req.Field), "msg")` — for `FluentResult.ValidationProblem`; locates
  the entry in the `errors[]` array by `field` and asserts its `message`.
- `ApiAssert.ValidationErrorAsync(response, nameof(Req.Field), expectedCode, "msg")` — same, but also asserts the
  per-field `code`.
- `ApiAssert.ValidationErrorAsync(response, nameof(Req.Field), errorCode)` — preferred: pass the `ErrorCode` catalog
  entry (e.g. `IdentityErrorCodes.InvalidCredentials`) and it asserts both `code` and `message` from one symbol.
- `ApiAssert.ValidationMessageAsync(response, "msg")` — for `FluentResult.ValidationMessage` (top-level
  `ProblemDetails.Detail`).
- `ApiAssert.ValidationMessageAsync(response, expectedCode, "msg")` — same, but also asserts the top-level `code`.
- `ApiAssert.ValidationMessageAsync(response, errorCode)` — preferred: asserts top-level `code` + `detail` from the
  `ErrorCode` entry.
- `ApiAssert.InternalErrorAsync(response, expectedCode)` / `ApiAssert.InternalErrorAsync(response, errorCode)` — asserts
  a 500 ProblemDetails carrying the top-level `code` (string or `ErrorCode` overload).

All overloads come in `IApiResponse` and `IApiResponse<T>` variants and read the new
`{ code, detail, errors:[{field, code, message}] }` shape (see [patterns.md → Error Codes](patterns.md#error-codes)).
