using Peritus.Api.Endpoints;
using Peritus.Api.Extensions.Setup;
using Peritus.Identity;
using Peritus.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services
    .AddProblemDetails()
    .AddValidation();

builder
    .ConfigureAccessors()
    .ConfigureAuth()
    .ConfigureCache()
    .ConfigureDatabase()
    .ConfigureOpenApi()
    .ConfigureOptions()
    .ConfigureTime();

builder.AddIdentityModule();

var app = builder.Build();

app.UseMiddlewares();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApiWithScalar();
}

app.MapDefaultEndpoints();

AuthEndpoints.Map(app);
UserEndpoints.Map(app);

app.RunSafe();
