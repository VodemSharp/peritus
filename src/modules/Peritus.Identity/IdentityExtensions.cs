using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
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
        builder.Services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();

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

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        // Auth
        SignUpFeature.MapEndpoint(app);
        SignInFeature.MapEndpoint(app);
        SignInTwoFactorFeature.MapEndpoint(app);
        SignInRecoveryCodeFeature.MapEndpoint(app);
        SignInGoogleFeature.MapEndpoint(app);
        SignOutFeature.MapEndpoint(app);
        TokenRefreshFeature.MapEndpoint(app);
        EmailSendConfirmationFeature.MapEndpoint(app);
        EmailConfirmFeature.MapEndpoint(app);
        PasswordResetSendFeature.MapEndpoint(app);
        PasswordResetFeature.MapEndpoint(app);

        // Accounts
        PasswordChangeFeature.MapEndpoint(app);
        SessionListFeature.MapEndpoint(app);
        SessionRevokeFeature.MapEndpoint(app);
        SessionRevokeAllFeature.MapEndpoint(app);
        TwoFactorEnableFeature.MapEndpoint(app);
        TwoFactorVerifySetupFeature.MapEndpoint(app);
        TwoFactorDisableFeature.MapEndpoint(app);
        TwoFactorGenerateRecoveryCodesFeature.MapEndpoint(app);
        PhoneNumberSendVerificationFeature.MapEndpoint(app);
        PhoneNumberVerifyFeature.MapEndpoint(app);

        // Profile
        ProfileGetFeature.MapEndpoint(app);
        ProfileUpdateFeature.MapEndpoint(app);

        return app;
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
