using System.ComponentModel;
using System.Text.Json.Serialization;
using Peritus.Primitives.Abstractions;
using Peritus.Primitives.Converters;

namespace Peritus.Types.Identity.Users;

[TypeConverter(typeof(StringValueTypeConverter<Password>))]
[JsonConverter(typeof(StringValueJsonConverter<Password>))]
public readonly record struct Password(string Value) : IStringValue
{
    public static implicit operator string(Password password) => password.Value;
}
