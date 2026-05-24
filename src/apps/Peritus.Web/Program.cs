using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Peritus.ApiClients.Abstractions;
using Peritus.ApiClients.Extensions;
using Peritus.ServiceDefaults;
using Peritus.Types.Http;
using Peritus.Web.Components;
using Peritus.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, CookieAuthenticationStateProvider>();

builder.Services.AddAuthentication("JwtCookie")
    .AddScheme<AuthenticationSchemeOptions, JwtCookieAuthenticationHandler>("JwtCookie", _ => { });
builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITokenStorage, CookieTokenStorage>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddIdentityApiClient(new Uri("https+http://api"), HttpClientName.IdentityApiRefresh);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
