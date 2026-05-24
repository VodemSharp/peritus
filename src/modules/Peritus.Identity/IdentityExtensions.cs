using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Peritus.Identity.Features.Accounts;
using Peritus.Identity.Features.Auth;
using Peritus.Identity.Features.Profile;
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
        builder.Services.AddScoped<IUserTokenService, UserTokenService>();
        builder.Services.AddScoped<ISessionValidator, SessionValidator>();

        // Features - Auth
        builder.Services.AddScoped<SignInFeature>();
        builder.Services.AddScoped<SignUpFeature>();
        builder.Services.AddScoped<TokenRefreshFeature>();
        builder.Services.AddScoped<SignInGoogleFeature>();
        builder.Services.AddScoped<SignInTwoFactorFeature>();
        builder.Services.AddScoped<SignInRecoveryCodeFeature>();
        builder.Services.AddScoped<EmailSendConfirmationFeature>();
        builder.Services.AddScoped<EmailConfirmFeature>();
        builder.Services.AddScoped<PasswordResetSendFeature>();
        builder.Services.AddScoped<PasswordResetFeature>();

        // Features - Accounts
        builder.Services.AddScoped<SignOutFeature>();
        builder.Services.AddScoped<PasswordChangeFeature>();
        builder.Services.AddScoped<SessionListFeature>();
        builder.Services.AddScoped<SessionRevokeFeature>();
        builder.Services.AddScoped<SessionRevokeAllFeature>();
        builder.Services.AddScoped<TwoFactorEnableFeature>();
        builder.Services.AddScoped<TwoFactorVerifySetupFeature>();
        builder.Services.AddScoped<TwoFactorDisableFeature>();
        builder.Services.AddScoped<TwoFactorGenerateRecoveryCodesFeature>();
        builder.Services.AddScoped<PhoneNumberSendVerificationFeature>();
        builder.Services.AddScoped<PhoneNumberVerifyFeature>();

        // Features - Profile
        builder.Services.AddScoped<ProfileGetFeature>();
        builder.Services.AddScoped<ProfileUpdateFeature>();

        // Options
        builder.Services.Configure<IdentityOptions>(builder.Configuration.GetSection("IdentityOptions"));

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
