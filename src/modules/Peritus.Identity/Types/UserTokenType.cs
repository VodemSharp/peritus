using System.ComponentModel;
using System.Text.Json.Serialization;
using Peritus.Primitives.Abstractions;
using Peritus.Primitives.Converters;

namespace Peritus.Identity.Types;

[TypeConverter(typeof(StringValueTypeConverter<UserTokenType>))]
[JsonConverter(typeof(StringValueJsonConverter<UserTokenType>))]
public readonly record struct UserTokenType(string Value) : IStringValue
{
    public static UserTokenType EmailConfirmation => new("EmailConfirmation");
    public static UserTokenType PasswordReset => new("PasswordReset");
    public static UserTokenType PhoneNumberVerification => new("PhoneNumberVerification");
    public static implicit operator string(UserTokenType userTokenType) => userTokenType.Value;
}
