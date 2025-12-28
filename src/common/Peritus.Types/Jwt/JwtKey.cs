using System.ComponentModel;
using System.Text.Json.Serialization;
using Peritus.Primitives.Abstractions;
using Peritus.Primitives.Converters;

namespace Peritus.Types.Jwt;

[TypeConverter(typeof(StringValueTypeConverter<JwtKey>))]
[JsonConverter(typeof(StringValueJsonConverter<JwtKey>))]
public readonly record struct JwtKey(string Value) : IStringValue
{
    public static implicit operator string(JwtKey key) => key.Value;
}
