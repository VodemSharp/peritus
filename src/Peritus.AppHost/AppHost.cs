using Projects;

#pragma warning disable ASPIRECOMPUTE003
#pragma warning disable ASPIREPIPELINES003

var builder = DistributedApplication.CreateBuilder(args);

var compose = builder.AddDockerComposeEnvironment("dokploy")
    .WithProperties(env => env.DashboardEnabled = true);

var registryEndpoint = builder.AddParameterFromConfiguration("registry-endpoint", "REGISTRY_ENDPOINT");
var registryRepository = builder.AddParameterFromConfiguration("registry-repository", "REGISTRY_REPOSITORY");
var registry = builder.AddContainerRegistry("ghcr", registryEndpoint, registryRepository);

var cache = builder.ExecutionContext.IsRunMode
    ? builder.AddValkey("cache").WithDataVolume().WithPersistence()
    : builder.AddConnectionString("cache");

var dbUsername = builder.AddParameter("dbUsername", true);
var dbPassword = builder.AddParameter("dbPassword", true);

var db = builder.ExecutionContext.IsRunMode
    ? builder.AddPostgres("postgres", dbUsername, dbPassword)
        .WithDataVolume()
        .WithLifetime(ContainerLifetime.Persistent)
        .AddDatabase("db", "peritus")
    : builder.AddConnectionString("db");

var migrator = builder.AddProject<Peritus_Migrator>("migrator")
    .WithReference(db)
    .WaitFor(db)
    .WithContainerRegistry(registry)
    .WithImagePushOptions(context =>
    {
        var version = Environment.GetEnvironmentVariable("APP_VERSION")
                      ?? builder.Environment.EnvironmentName.ToLowerInvariant();
        context.Options.RemoteImageTag = version;
    });

var api = builder.AddProject<Peritus_Api>("api")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WithReference(db)
    .WaitFor(migrator)
    .WithContainerRegistry(registry)
    .WithImagePushOptions(context =>
    {
        var version = Environment.GetEnvironmentVariable("APP_VERSION")
                      ?? builder.Environment.EnvironmentName.ToLowerInvariant();
        context.Options.RemoteImageTag = version;
    });

var web = builder.AddProject<Peritus_Web>("web")
    .WithExternalHttpEndpoints()
    .WithReference(api)
    .WaitFor(api)
    .WithContainerRegistry(registry)
    .WithImagePushOptions(context =>
    {
        var version = Environment.GetEnvironmentVariable("APP_VERSION")
                      ?? builder.Environment.EnvironmentName.ToLowerInvariant();
        context.Options.RemoteImageTag = version;
    });

builder.Build().Run();

#pragma warning restore ASPIREPIPELINES003
#pragma warning restore ASPIRECOMPUTE003
