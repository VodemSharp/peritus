using System.ComponentModel;
using System.Text.Json.Serialization;
using Peritus.Primitives.Abstractions;
using Peritus.Primitives.Converters;

namespace Peritus.Types.Identity.Users;

[TypeConverter(typeof(GuidValueTypeConverter<UserId>))]
[JsonConverter(typeof(GuidValueJsonConverter<UserId>))]
public readonly record struct UserId(Guid Value) : IGuidValue
{
    public static implicit operator Guid(UserId userId) => userId.Value;
}
