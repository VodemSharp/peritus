# Peritus — Testing Guide

[← Back to main guide](../CLAUDE.md)

## Test Architecture

```
InfrastructureFixture (IAsyncLifetime)
  ├─ Creates Aspire AppHost with PostgreSQL + Valkey containers
  ├─ Removes api/web resources (tests create their own WebApplicationFactory)
  ├─ Waits for migrator to finish
  ├─ Provides connection strings
  └─ Shared across all tests in a class via IClassFixture

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
        var credentials = await CreateUserAsync(_ct);
        var tokens = await SignInAsync(credentials);
        var api = CreateIdentityApi(tokens.AccessToken);

        var response = await api.SomeEndpointAsync(new SomeRequest { ... }, _ct);

        ApiAssert.Success(response, result =>
        {
            Assert.Equal("expected", result.Something);
        });
    }
}
```

### Test Helpers

- `UserCredentials.Create()` generates a unique email/password pair using `Guid.CreateVersion7()`. Always use this
  instead of hardcoded values.
- `ApiAssert.SuccessAsync(...)` is available for assertions that need `await` (e.g., querying the database after an API
  call).
- `PeritusApplicationFactory` is anchored to `IApiMarker` (`src/apps/Peritus.Api/IApiMarker.cs`) for
  `WebApplicationFactory<T>` discovery.
