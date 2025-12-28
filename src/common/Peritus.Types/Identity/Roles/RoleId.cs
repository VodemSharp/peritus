using System.ComponentModel;
using System.Text.Json.Serialization;
using Peritus.Primitives.Abstractions;
using Peritus.Primitives.Converters;

namespace Peritus.Types.Identity.Roles;

[TypeConverter(typeof(GuidValueTypeConverter<RoleId>))]
[JsonConverter(typeof(GuidValueJsonConverter<RoleId>))]
public readonly record struct RoleId(Guid Value) : IGuidValue
{
    public static implicit operator Guid(RoleId roleId) => roleId.Value;
}
