using System.ComponentModel;
using System.Text.Json.Serialization;
using Peritus.Primitives.Abstractions;
using Peritus.Primitives.Converters;

namespace Peritus.Identity.Types;

[TypeConverter(typeof(StringValueTypeConverter<UserSessionProvider>))]
[JsonConverter(typeof(StringValueJsonConverter<UserSessionProvider>))]
public readonly record struct UserSessionProvider(string Value) : IStringValue
{
    public static UserSessionProvider Credentials => new("Credentials");
    public static implicit operator string(UserSessionProvider userSessionProvider) => userSessionProvider.Value;
}
