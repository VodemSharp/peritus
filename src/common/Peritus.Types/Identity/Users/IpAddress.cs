using System.ComponentModel;
using System.Text.Json.Serialization;
using Peritus.Primitives.Abstractions;
using Peritus.Primitives.Converters;

namespace Peritus.Types.Identity.Users;

[TypeConverter(typeof(StringValueTypeConverter<IpAddress>))]
[JsonConverter(typeof(StringValueJsonConverter<IpAddress>))]
public readonly record struct IpAddress(string Value) : IStringValue
{
    public static implicit operator string(IpAddress ipAddress) => ipAddress.Value;
}
