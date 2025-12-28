using System.ComponentModel;
using System.Text.Json.Serialization;
using Peritus.Primitives.Abstractions;
using Peritus.Primitives.Converters;

namespace Peritus.Identity.Types;

[TypeConverter(typeof(GuidValueTypeConverter<UserSessionId>))]
[JsonConverter(typeof(GuidValueJsonConverter<UserSessionId>))]
public readonly record struct UserSessionId(Guid Value) : IGuidValue
{
    public static implicit operator Guid(UserSessionId userSessionId) => userSessionId.Value;
}
