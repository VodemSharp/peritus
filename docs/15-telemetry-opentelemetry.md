# Telemetry: OpenTelemetry setup

Telemetry is configured via `Peritus.ServiceDefaults`, applied to hosts to emit traces and metrics with sensible defaults.

## Service defaults extension
- `AddServiceDefaults` wires OpenTelemetry logging/tracing/metrics and HTTP resilience/discovery:
  ```csharp
  public TBuilder AddServiceDefaults()
  {
      builder.ConfigureOpenTelemetry();
      builder.AddDefaultHealthChecks();
      builder.Services.AddServiceDiscovery();
      builder.Services.ConfigureHttpClientDefaults(http =>
      {
          http.AddStandardResilienceHandler();
          http.AddServiceDiscovery();
      });
      return builder;
  }
  ```
  @/src/host/Peritus.ServiceDefaults/Extensions.cs#43-69

- OpenTelemetry configuration adds logging enrichment and ASP.NET Core/HttpClient/Runtime instrumentation, excluding health endpoints from traces:
  ```csharp
  builder.Logging.AddOpenTelemetry(logging =>
  {
      logging.IncludeFormattedMessage = true;
      logging.IncludeScopes = true;
  });

  builder.Services.AddOpenTelemetry()
      .WithMetrics(metrics =>
      {
          metrics.AddAspNetCoreInstrumentation()
              .AddHttpClientInstrumentation()
              .AddRuntimeInstrumentation();
      })
      .WithTracing(tracing =>
      {
          tracing.AddSource(builder.Environment.ApplicationName)
              .AddAspNetCoreInstrumentation(options =>
                  options.Filter = context =>
                      !context.Request.Path.StartsWithSegments("/health")
                      && !context.Request.Path.StartsWithSegments("/alive"))
              .AddHttpClientInstrumentation();
      });

  builder.AddOpenTelemetryExporters();
  ```
  @/src/host/Peritus.ServiceDefaults/Extensions.cs#71-103

- OTLP exporter is enabled when `OTEL_EXPORTER_OTLP_ENDPOINT` is present:
  ```csharp
  if (useOtlpExporter)
  {
      builder.Services.AddOpenTelemetry().UseOtlpExporter();
  }
  ```
  @/src/host/Peritus.ServiceDefaults/Extensions.cs#105-112

## Health endpoints
- Default health checks are registered and mapped to `/health` (readiness) and `/alive` (liveness) in development environments.@/src/host/Peritus.ServiceDefaults/Extensions.cs#18-40

## Usage
- `builder.AddServiceDefaults();` is called in `Program.cs` for the API, applying telemetry, resilience, and health checks to the service.
  @/src/api/Peritus.Api/Program.cs#8-21

## Notes
- Health requests are excluded from tracing to keep noise low.
- HTTP clients gain resilience and service discovery automatically through the defaults.
