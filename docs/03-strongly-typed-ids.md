# Strongly-typed IDs and converters

Peritus uses value object IDs (GUID/string) everywhere—from contracts through persistence. Converters make them work with JSON, EF Core, and TypeConverter.

## Value object definitions
- Example IDs implement `IGuidValue` with converters attached via attributes:
  ```csharp
  [TypeConverter(typeof(GuidValueTypeConverter<UserId>))]
  [JsonConverter(typeof(GuidValueJsonConverter<UserId>))]
  public readonly record struct UserId(Guid Value) : IGuidValue
  {
      public static implicit operator Guid(UserId userId) => userId.Value;
  }
  ```
  @/src/common/Peritus.Types/Identity/Users/UserId.cs#1-13
- Other IDs follow the same pattern (e.g., `RoleId`, `AccessTokenId.Create()` for Guid v7 generation).@/src/common/Peritus.Types/Identity/Roles/RoleId.cs#1-13 @/src/common/Peritus.Types/Tokens/AccessTokenId.cs#1-18

## JSON converters
- `GuidValueJsonConverter<T>` reads/writes Guid-based value objects; `StringValueJsonConverter<T>` does the same for strings.
  ```csharp
  public sealed class GuidValueJsonConverter<T> : JsonConverter<T> where T : struct, IGuidValue
  {
      public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
      {
          var value = reader.GetGuid();
          return (T)Activator.CreateInstance(typeof(T), value)!;
      }

      public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
      {
          writer.WriteStringValue(value.Value);
      }
  }
  ```
  @/src/common/Peritus.Primitives/Converters/GuidValueJsonConverter.cs#7-19

## Type converters (binding/DI/options)
- `GuidValueTypeConverter<T>` allows binding from string/Guid and back for configuration, routing, and model binding.
  ```csharp
  public sealed class GuidValueTypeConverter<T> : TypeConverter where T : struct, IGuidValue
  {
      public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => sourceType == typeof(string) || sourceType == typeof(Guid) || base.CanConvertFrom(context, sourceType);
      public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) => value switch { string s when Guid.TryParse(s, out var guid) => (T)Activator.CreateInstance(typeof(T), guid)!, Guid g => (T)Activator.CreateInstance(typeof(T), g)!, _ => base.ConvertFrom(context, culture, value) };
      public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) => destinationType == typeof(string) || destinationType == typeof(Guid) || base.CanConvertTo(context, destinationType);
      public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
      {
          if (value is not T t) return base.ConvertTo(context, culture, value, destinationType);
          if (destinationType == typeof(string)) return t.Value.ToString();
          if (destinationType == typeof(Guid)) return t.Value;
          return base.ConvertTo(context, culture, value, destinationType);
      }
  }
  ```
  @/src/common/Peritus.Primitives/Converters/GuidValueTypeConverter.cs#7-54

## EF Core value conversions
- Global configuration registers converters for all strong types:
  ```csharp
  modelBuilder.Model.ConfigureGuidValue<UserId>();
  modelBuilder.Model.ConfigureStringValue<SomeStringId>();
  ```
  via `ConversionExtensions` that apply `GuidValueConverter<T>` / `StringValueConverter<T>` across the model.@/src/common/Peritus.Persistence/Extensions/ConversionExtensions.cs#8-23
- `GuidValueConverter<T>` maps value object ⇆ Guid using a compiled constructor factory; throws if the type lacks a `Guid` ctor.@/src/common/Peritus.Persistence/ValueConverters/GuidValueConverter.cs#7-21

## How to add a new ID
1) Define a record struct implementing `IGuidValue` or `IStringValue` with a single-parameter constructor.
2) Decorate with `[TypeConverter]` + `[JsonConverter]` using the provided converter types.
3) Register conversions in your `DbContext` (`ConfigureGuidValue<T>()`) and `HasGuidValueGenerator()` for keys if needed.
4) Use the strong type in contracts, services, claims, and persistence—avoid `Guid.Parse`/string literals.
