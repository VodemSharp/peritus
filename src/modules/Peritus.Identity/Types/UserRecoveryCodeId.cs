using System.ComponentModel;
using System.Text.Json.Serialization;
using Peritus.Primitives.Abstractions;
using Peritus.Primitives.Converters;

namespace Peritus.Identity.Types;

[TypeConverter(typeof(GuidValueTypeConverter<UserRecoveryCodeId>))]
[JsonConverter(typeof(GuidValueJsonConverter<UserRecoveryCodeId>))]
public readonly record struct UserRecoveryCodeId(Guid Value) : IGuidValue
{
    public static implicit operator Guid(UserRecoveryCodeId userRecoveryCodeId) => userRecoveryCodeId.Value;
}
