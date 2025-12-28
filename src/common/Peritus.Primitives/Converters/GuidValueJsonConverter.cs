using System.Text.Json;
using System.Text.Json.Serialization;
using Peritus.Primitives.Abstractions;

namespace Peritus.Primitives.Converters;

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
