using System.ComponentModel;
using System.Text.Json.Serialization;
using Peritus.Primitives.Abstractions;
using Peritus.Primitives.Converters;

namespace Peritus.Types.Tokens;

[TypeConverter(typeof(StringValueTypeConverter<AccessToken>))]
[JsonConverter(typeof(StringValueJsonConverter<AccessToken>))]
public readonly record struct AccessToken(string Value) : IStringValue
{
    public static implicit operator string(AccessToken accessToken) => accessToken.Value;
}
