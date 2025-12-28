using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Peritus.Api.Middlewares;
using Peritus.Guard.Options;
using Peritus.Guard.Services;
using Peritus.Guard.Services.Abstractions;

namespace Peritus.Api.Extensions.Setup;

public static class AuthExtensions
{
    public static WebApplicationBuilder ConfigureAuth(this WebApplicationBuilder builder)
    {
        builder.Services.AddJwtAuthentication(builder.Configuration.GetSection(nameof(AccessTokenOptions)));
        builder.Services.AddAuthorization();

        builder.Services.AddTransient(typeof(IPasswordHasher<>), typeof(PasswordHasher<>));
        builder.Services.AddTransient<ITokenRevoker, TokenRevoker>();
        builder.Services.AddTransient<JwtBlacklistMiddleware>();

        return builder;
    }

    private static AuthenticationBuilder AddJwtAuthentication(this IServiceCollection services,
        IConfigurationSection accessTokenSection, Action<JwtBearerOptions>? bearerOptions = null)
    {
        var accessTokenOptions = accessTokenSection.Get<AccessTokenOptions>()!;
        services.Configure<AccessTokenOptions>(accessTokenSection);

        var authenticationBuilder =
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = accessTokenOptions.Issuer,
                    ValidAudience = accessTokenOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(accessTokenOptions.Key)),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];

                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };

                bearerOptions?.Invoke(options);
            });

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<TimeProvider>((options, timeProvider) =>
            {
                options.TokenValidationParameters.LifetimeValidator = (notBefore, expires, _, parameters) =>
                {
                    var now = timeProvider.GetUtcNow().UtcDateTime;
                    var skew = parameters.ClockSkew;

                    if (notBefore.HasValue && now < notBefore.Value.Subtract(skew))
                    {
                        return false;
                    }

                    // ReSharper disable once ConvertIfStatementToReturnStatement
                    if (expires.HasValue && now > expires.Value.Add(skew))
                    {
                        return false;
                    }

                    return true;
                };
            });

        return authenticationBuilder;
    }
}
