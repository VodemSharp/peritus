using System.ComponentModel;
using System.Text.Json.Serialization;
using Peritus.Primitives.Abstractions;
using Peritus.Primitives.Converters;

namespace Peritus.Types.Identity.Users;

[TypeConverter(typeof(StringValueTypeConverter<Email>))]
[JsonConverter(typeof(StringValueJsonConverter<Email>))]
public readonly record struct Email(string Value) : IStringValue
{
    public static implicit operator string(Email email) => email.Value;
}
