using Projects;

var builder = DistributedApplication.CreateBuilder(args);

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

builder.Build().Run();
