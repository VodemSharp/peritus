# Strong IDs: System.Text.Json converters

Value objects serialize/deserialize as primitives via custom JSON converters.

## Guid-based converter
- `GuidValueJsonConverter<T>` reads a GUID from JSON and constructs the value object; writes the underlying `Value` as a string.
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

## String-based converter
- `StringValueJsonConverter<T>` does the same for string value objects.
  ```csharp
  public sealed class StringValueJsonConverter<T> : JsonConverter<T> where T : struct, IStringValue
  {
      public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
      {
          var value = reader.GetString()!;
          return (T)Activator.CreateInstance(typeof(T), value)!;
      }

      public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
      {
          writer.WriteStringValue(value.Value);
      }
  }
  ```
  @/src/common/Peritus.Primitives/Converters/StringValueJsonConverter.cs#7-18

## Usage
- Apply `[JsonConverter(typeof(GuidValueJsonConverter<UserId>))]` (or string variant) on your value type.
- Contracts and APIs can use strong IDs directly; serialization stays compatible with primitive GUID/string payloads.
