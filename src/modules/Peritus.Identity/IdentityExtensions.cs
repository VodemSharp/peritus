using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Peritus.Identity.Features.Auth;
using Peritus.Identity.Features.Users;
using Peritus.Identity.Options;
using Peritus.Identity.Persistence;
using Peritus.Identity.Services;
using Peritus.Identity.Services.Abstractions;
using Peritus.Persistence.Interceptors;

namespace Peritus.Identity;

public static class IdentityExtensions
{
    public static WebApplicationBuilder AddIdentityModule(this WebApplicationBuilder builder)
    {
        // Services
        builder.Services.AddScoped<IUserSessionService, UserSessionService>();
        builder.Services.AddScoped<ITokenService, TokenService>();
        builder.Services.AddScoped<IUserService, UserService>();

        // Features - Auth
        builder.Services.AddScoped<RefreshTokenFeature>();
        builder.Services.AddScoped<SignInFeature>();
        builder.Services.AddScoped<SignOutFeature>();
        builder.Services.AddScoped<SignUpFeature>();

        // Features - Users
        builder.Services.AddScoped<GetCurrentUserFeature>();

        // Options
        builder.Services.Configure<RefreshTokenOptions>(builder.Configuration.GetSection("RefreshTokenOptions"));

        builder.AddIdentityDbContext();

        return builder;
    }

    public static IHostApplicationBuilder AddIdentityDbContext(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDbContext<IdentityDbContext>((sp, options) =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString("db"),
                optionsBuilder => optionsBuilder.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));

            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

            options.AddInterceptors(
                sp.GetRequiredService<PerformanceInterceptor>(),
                sp.GetRequiredService<TimestampInterceptor>()
            );

            options.UseSnakeCaseNamingConvention();
        });

        builder.EnrichNpgsqlDbContext<IdentityDbContext>();
        return builder;
    }
}
