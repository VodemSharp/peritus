# Strong IDs: TypeConverter support

Value objects integrate with configuration/model binding via custom `TypeConverter`s.

## Guid-based TypeConverter
- Enables conversion from string/Guid to strong type and back (for DI, options, routing):
  ```csharp
  public sealed class GuidValueTypeConverter<T> : TypeConverter where T : struct, IGuidValue
  {
      public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
      {
          return sourceType == typeof(string)
                 || sourceType == typeof(Guid)
                 || base.CanConvertFrom(context, sourceType);
      }

      public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
      {
          return value switch
          {
              string s when Guid.TryParse(s, out var guid) => (T)Activator.CreateInstance(typeof(T), guid)!,
              Guid g => (T)Activator.CreateInstance(typeof(T), g)!,
              _ => base.ConvertFrom(context, culture, value)
          };
      }

      public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
      {
          return destinationType == typeof(string)
                 || destinationType == typeof(Guid)
                 || base.CanConvertTo(context, destinationType);
      }

      public override object? ConvertTo(
          ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
      {
          if (value is not T t)
          {
              return base.ConvertTo(context, culture, value, destinationType);
          }

          if (destinationType == typeof(string))
          {
              return t.Value.ToString();
          }

          if (destinationType == typeof(Guid))
          {
              return t.Value;
          }

          return base.ConvertTo(context, culture, value, destinationType);
      }
  }
  ```
  @/src/common/Peritus.Primitives/Converters/GuidValueTypeConverter.cs#7-54

## Applying to IDs
- IDs declare the converter via attribute:
  ```csharp
  [TypeConverter(typeof(GuidValueTypeConverter<UserId>))]
  public readonly record struct UserId(Guid Value) : IGuidValue
  {
      public static implicit operator Guid(UserId userId) => userId.Value;
  }
  ```
  @/src/common/Peritus.Types/Identity/Users/UserId.cs#1-13

## Why it matters
- Allows binding of strong IDs from configuration/environment variables, query/path values, and options without manual parsing.
