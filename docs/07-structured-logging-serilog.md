# Structured logging with Serilog (console + Loki)

Logging is wired via a shared configurator that enriches events with service/env and can forward to Grafana Loki.

## Serilog configurator
- `SerilogConfigurator.Configure` builds a logger with console sink and optional Loki sink:
  ```csharp
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
  ```
  @/src/common/Peritus.Logging/SerilogConfigurator.cs#9-29

## How to use
- Each app/service can call `SerilogConfigurator.Configure(serviceName, env, configuration.GetSection("Serilog"), lokiConnection)` to get a preconfigured logger.
- The Loki sink is opt-in via connection string; console logging is always on.

## Why it matters
- Enriched properties (`service`, `environment`) become structured labels for correlation in Loki or any sink.
- Console minimum level defaults to `Verbose`, while Loki is restricted to `Warning` and above to reduce noise.
