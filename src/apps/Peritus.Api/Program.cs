using Peritus.Api.Extensions.Setup;
using Peritus.Identity;
using Peritus.Messaging;
using Peritus.Notification;
using Peritus.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services
    .AddProblemDetails()
    .AddValidation()
    .AddHttpClient();

builder
    .ConfigureAccessors()
    .ConfigureAuth()
    .ConfigureCache()
    .ConfigureDatabase()
    .ConfigureOpenApi()
    .ConfigureOptions()
    .ConfigureTime();

builder.Services.AddMediator();
builder.AddIdentityModule();
builder.AddNotificationModule();

var app = builder.Build();

app.UseMiddlewares();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApiWithScalar();
}

app.MapDefaultEndpoints();
app.MapIdentityEndpoints();

app.Run();
