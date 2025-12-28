using Microsoft.AspNetCore.Identity;
using Peritus.Identity;
using Peritus.Identity.Migrator;
using Peritus.Identity.Migrator.Options;
using Peritus.Persistence.Extensions;
using Peritus.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.Configure<AdminOptions>(builder.Configuration.GetSection(nameof(AdminOptions)));

builder.Services.AddInterceptors();
builder.Services.AddTransient(typeof(IPasswordHasher<>), typeof(PasswordHasher<>));
builder.Services.AddHostedService<Worker>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

builder.AddIdentityDbContext();

var host = builder.Build();
host.Run();
