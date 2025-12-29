# Containers and infrastructure (Aspire, Postgres, Valkey)

Aspire AppHost provisions infrastructure for the solution and orchestrates projects.

## AppHost composition
- `AppHost.cs` defines Valkey cache with persistence, Postgres with data volume, database `db`, migrator, and API project dependencies:
  ```csharp
  var cache = builder.AddValkey("cache")
      .WithDataVolume()
      .WithPersistence();

  var dbUsername = builder.AddParameter("dbUsername", true);
  var dbPassword = builder.AddParameter("dbPassword", true);

  var postgres = builder.AddPostgres("postgres", dbUsername, dbPassword)
      .WithDataVolume()
      .WithLifetime(ContainerLifetime.Persistent);

  var db = postgres.AddDatabase("db", "peritus");

  var identityMigrator = builder.AddProject<Peritus_Identity_Migrator>("identity-migrator")
      .WithReference(db)
      .WaitFor(db);

  var api = builder.AddProject<Peritus_Api>("api")
      .WithHttpHealthCheck("/health")
      .WithReference(cache)
      .WithReference(db)
      .WaitFor(identityMigrator);
  ```
  @/src/host/Peritus.AppHost/AppHost.cs#5-26
- Health and readiness are wired via ServiceDefaults on the API side; migrator must finish before API serves traffic.

## Environment parameters
- Database credentials are parameters (`dbUsername`, `dbPassword`), enabling secure injection in local/CI environments.

## Data persistence
- Valkey and Postgres are set with data volumes to persist across container restarts.

## How to run
- `dotnet run --project src/host/Peritus.AppHost/Peritus.AppHost.csproj` will start the distributed application with the defined resources and projects.
