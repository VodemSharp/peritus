using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;

namespace Peritus.Logging;

public static class SerilogConfigurator
{
    public static Logger Configure(string serviceName, string envName, IConfigurationSection serilogSection,
        string? lokiConnection = null, LogEventLevel consoleMinimumLevel = LogEventLevel.Verbose)
    {
        var loggerConfig = new LoggerConfiguration()
            .WriteTo.Console(consoleMinimumLevel)
            .Enrich.WithProperty("service", serviceName)
            .Enrich.WithProperty("environment", envName);

        if (lokiConnection != null)
        {
            loggerConfig.WriteTo.GrafanaLoki(
                lokiConnection,
                propertiesAsLabels: ["service", "environment", "level"],
                restrictedToMinimumLevel: LogEventLevel.Warning);
        }

        loggerConfig.ReadFrom.Configuration(serilogSection);
        return loggerConfig.CreateLogger();
    }
}
