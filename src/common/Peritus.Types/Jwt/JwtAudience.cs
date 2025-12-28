using System.ComponentModel;
using System.Text.Json.Serialization;
using Peritus.Primitives.Abstractions;
using Peritus.Primitives.Converters;

namespace Peritus.Types.Jwt;

[TypeConverter(typeof(StringValueTypeConverter<JwtAudience>))]
[JsonConverter(typeof(StringValueJsonConverter<JwtAudience>))]
public readonly record struct JwtAudience(string Value) : IStringValue
{
    public static implicit operator string(JwtAudience audience) => audience.Value;
}
