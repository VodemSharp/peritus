using Microsoft.AspNetCore.Components.Authorization;
using Peritus.ApiClients.Abstractions;
using Peritus.ApiClients.Extensions;
using Peritus.Types.Http;
using Peritus.Web.Components;
using Peritus.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, CookieAuthenticationStateProvider>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITokenStorage, CookieTokenStorage>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddServiceDiscovery();
builder.Services.ConfigureHttpClientDefaults(static http => http.AddServiceDiscovery());

builder.Services.AddIdentityApiClient(new Uri("http://api"), HttpClientName.IdentityApiRefresh);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
