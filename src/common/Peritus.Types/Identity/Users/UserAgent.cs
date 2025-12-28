using System.ComponentModel;
using System.Text.Json.Serialization;
using Peritus.Primitives.Abstractions;
using Peritus.Primitives.Converters;

namespace Peritus.Types.Identity.Users;

[TypeConverter(typeof(StringValueTypeConverter<UserAgent>))]
[JsonConverter(typeof(StringValueJsonConverter<UserAgent>))]
public readonly record struct UserAgent(string Value) : IStringValue
{
    public static implicit operator string(UserAgent userAgent) => userAgent.Value;
}
