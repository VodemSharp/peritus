using System.ComponentModel;
using System.Text.Json.Serialization;
using Peritus.Primitives.Abstractions;
using Peritus.Primitives.Converters;

namespace Peritus.Identity.Types;

[TypeConverter(typeof(StringValueTypeConverter<UserSessionStatus>))]
[JsonConverter(typeof(StringValueJsonConverter<UserSessionStatus>))]
public readonly record struct UserSessionStatus(string Value) : IStringValue
{
    public static UserSessionStatus Confirmed => new("Confirmed");
    public static UserSessionStatus Terminated => new("Terminated");
    public static implicit operator string(UserSessionStatus userSessionStatus) => userSessionStatus.Value;
}
