using System.ComponentModel;
using System.Text.Json.Serialization;
using Peritus.Primitives.Abstractions;
using Peritus.Primitives.Converters;

namespace Peritus.Types.Identity.Roles;

[TypeConverter(typeof(StringValueTypeConverter<RoleName>))]
[JsonConverter(typeof(StringValueJsonConverter<RoleName>))]
public readonly record struct RoleName(string Value) : IStringValue
{
    public static RoleName Admin => new("admin");
    public static implicit operator string(RoleName roleName) => roleName.Value;
}
