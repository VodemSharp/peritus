using Peritus.Logging;
using Serilog;
using Serilog.Events;

namespace Peritus.Api.Extensions.Setup;

public static class LoggingExtensions
{
    public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
    {
        Log.Logger = SerilogConfigurator.Configure(
            builder.Environment.ApplicationName,
            builder.Environment.EnvironmentName,
            builder.Configuration.GetSection("Serilog"),
            builder.Configuration.GetConnectionString("LokiConnection"),
            LogEventLevel.Warning);

        builder.Host.UseSerilog();

        return builder;
    }
}
