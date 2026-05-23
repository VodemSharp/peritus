using Peritus.Api.Middlewares;

namespace Peritus.Api.Extensions.Setup;

public static class MiddlewareExtensions
{
    public static WebApplication UseMiddlewares(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler();
        }
        else
        {
            app.UseHttpsRedirection();
        }

        app.UseForwardedHeaders()
            .UseMiddleware<CultureMiddleware>()
            .UseAuthentication()
            .UseAuthorization()
            .UseMiddleware<SessionValidationMiddleware>();

        return app;
    }
}
