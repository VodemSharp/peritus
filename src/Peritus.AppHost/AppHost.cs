using Projects;

#pragma warning disable ASPIRECOMPUTE003
#pragma warning disable ASPIREPIPELINES003

var builder = DistributedApplication.CreateBuilder(args);

var compose = builder.AddDockerComposeEnvironment("dokploy")
    .WithProperties(env => env.DashboardEnabled = true);

var cache = builder.ExecutionContext.IsRunMode
    ? builder.AddValkey("cache")
        .WithDataVolume()
        .WithPersistence()
    : builder.AddConnectionString("cache");

var db = builder.ExecutionContext.IsRunMode
    ? builder.AddPostgres("postgres")
        .WithDataVolume()
        .WithLifetime(ContainerLifetime.Persistent)
        .AddDatabase("db", "peritus")
    : builder.AddConnectionString("db");

var migrator = builder.AddProject<Peritus_Migrator>("migrator")
    .WithReference(db)
    .WaitFor(db);

var api = builder.AddProject<Peritus_Api>("api")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WithReference(db)
    .WaitForCompletion(migrator);

var web = builder.AddProject<Peritus_Web>("web")
    .WithExternalHttpEndpoints()
    .WithReference(api)
    .WaitFor(api);

if (!builder.ExecutionContext.IsRunMode)
{
    var registryEndpoint = builder.AddParameterFromConfiguration("registry-endpoint", "REGISTRY_ENDPOINT");
    var registryRepository = builder.AddParameterFromConfiguration("registry-repository", "REGISTRY_REPOSITORY");
    var registry = builder.AddContainerRegistry("ghcr", registryEndpoint, registryRepository);

    string GetVersion() => Environment.GetEnvironmentVariable("APP_VERSION")
                           ?? builder.Environment.EnvironmentName.ToLowerInvariant();

    migrator.WithContainerRegistry(registry)
        .WithImagePushOptions(context => context.Options.RemoteImageTag = GetVersion());

    api.WithContainerRegistry(registry)
        .WithImagePushOptions(context => context.Options.RemoteImageTag = GetVersion());

    web.WithContainerRegistry(registry)
        .WithImagePushOptions(context => context.Options.RemoteImageTag = GetVersion());
}

builder.Build().Run();

#pragma warning restore ASPIREPIPELINES003
#pragma warning restore ASPIRECOMPUTE003
