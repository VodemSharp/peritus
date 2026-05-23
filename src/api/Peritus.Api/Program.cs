using Peritus.Api.Endpoints;
using Peritus.Api.Endpoints.Accounts;
using Peritus.Api.Endpoints.Auth;
using Peritus.Api.Extensions.Setup;
using Peritus.Identity;
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

builder.AddIdentityModule();
builder.AddNotificationModule();

var app = builder.Build();

app.UseMiddlewares();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApiWithScalar();
}

app.MapDefaultEndpoints();

// Auth
SignInEndpoints.Map(app);
SignUpEndpoints.Map(app);
SignOutEndpoints.Map(app);
RefreshTokenEndpoints.Map(app);
PasswordResetEndpoints.Map(app);
EmailConfirmationEndpoints.Map(app);

// Accounts
ChangePasswordEndpoints.Map(app);
SessionEndpoints.Map(app);
TwoFactorEndpoints.Map(app);
PhoneNumberVerificationEndpoints.Map(app);

ProfileEndpoints.Map(app);

app.Run();
