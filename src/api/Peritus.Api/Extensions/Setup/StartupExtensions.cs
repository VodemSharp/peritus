using Serilog;

namespace Peritus.Api.Extensions.Setup;

public static class StartupExtensions
{
    public static WebApplication RunSafe(this WebApplication app)
    {
        try
        {
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly.");
        }
        finally
        {
            Log.CloseAndFlush();
        }

        return app;
    }
}
