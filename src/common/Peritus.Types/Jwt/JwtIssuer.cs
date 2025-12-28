using System.ComponentModel;
using System.Text.Json.Serialization;
using Peritus.Primitives.Abstractions;
using Peritus.Primitives.Converters;

namespace Peritus.Types.Jwt;

[TypeConverter(typeof(StringValueTypeConverter<JwtIssuer>))]
[JsonConverter(typeof(StringValueJsonConverter<JwtIssuer>))]
public readonly record struct JwtIssuer(string Value) : IStringValue
{
    public static implicit operator string(JwtIssuer issuer) => issuer.Value;
}
