# Persistence: EF Core conventions, converters, interceptors

Peritus centralizes EF Core setup for strong IDs, snake_case naming, value generation, and auditing/perf interceptors.

## DbContext wiring (Identity module)
- The module registers `IdentityDbContext` with Npgsql, split queries, no-tracking by default, interceptors, and snake_case naming:
  ```csharp
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
  ```
  @/src/modules/Peritus.Identity/IdentityExtensions.cs#42-57

## Strong ID conversions
- Global registration of converters for `IGuidValue`/`IStringValue` types:
  ```csharp
  modelBuilder.Model.ConfigureGuidValue<UserId>();
  // ...
  return builder.Properties<T>().HaveConversion<GuidValueConverter<T>>();
  ```
  @/src/common/Peritus.Persistence/Extensions/ConversionExtensions.cs#8-23
- `GuidValueConverter<T>` maps value objects ⇆ Guid via a compiled ctor factory and throws if the type lacks a Guid ctor.@/src/common/Peritus.Persistence/ValueConverters/GuidValueConverter.cs#7-21

## Key generation
- `GuidValueGenerator<T>` creates Guid v7 values for key properties, using the same ctor factory to produce the strong type.@/src/common/Peritus.Persistence/ValueGenerators/GuidValueGenerator.cs#8-29

## Auditing interceptor
- `TimestampInterceptor` sets `CreatedAt`/`UpdatedAt` on entities implementing `ICreatedEntity`/`IUpdatedEntity` during SaveChanges/SaveChangesAsync, preserving original `CreatedAt` on updates and stamping `UpdatedAt` on add/modify.
  @/src/common/Peritus.Persistence/Interceptors/TimestampInterceptor.cs#7-68

## Performance interceptor
- `PerformanceInterceptor` logs a warning for queries exceeding the threshold (default 1s) on `ReaderExecuted` (sync/async).
  @/src/common/Peritus.Persistence/Interceptors/PerformanceInterceptor.cs#7-35

## Naming convention
- `UseSnakeCaseNamingConvention()` ensures table/column naming matches PostgreSQL snake_case.

## How to extend
- Add more interceptors via `options.AddInterceptors(...)`.
- Register additional value converters if you introduce new value object shapes.
- Adjust query tracking or splitting behavior per module as needed.
