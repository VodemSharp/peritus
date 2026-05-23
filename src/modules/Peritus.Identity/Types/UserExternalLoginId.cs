using System.ComponentModel;
using System.Text.Json.Serialization;
using Peritus.Primitives.Abstractions;
using Peritus.Primitives.Converters;

namespace Peritus.Identity.Types;

[TypeConverter(typeof(GuidValueTypeConverter<UserExternalLoginId>))]
[JsonConverter(typeof(GuidValueJsonConverter<UserExternalLoginId>))]
public readonly record struct UserExternalLoginId(Guid Value) : IGuidValue
{
    public static implicit operator Guid(UserExternalLoginId userExternalLoginId) => userExternalLoginId.Value;
}
