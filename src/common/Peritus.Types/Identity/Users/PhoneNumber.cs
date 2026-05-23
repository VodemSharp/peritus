using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Peritus.Primitives.Abstractions;
using Peritus.Primitives.Converters;

namespace Peritus.Types.Identity.Users;

[TypeConverter(typeof(StringValueTypeConverter<PhoneNumber>))]
[JsonConverter(typeof(StringValueJsonConverter<PhoneNumber>))]
public readonly partial record struct PhoneNumber(string Value) : IStringValue
{
    public static implicit operator string(PhoneNumber phoneNumber) => phoneNumber.Value;

    // E.164 format: '+' followed by 7-15 digits
    [GeneratedRegex(@"^\+[1-9]\d{6,14}$")]
    private static partial Regex E164Regex();

    public static bool IsValid(string? phoneNumber)
    {
        return !string.IsNullOrWhiteSpace(phoneNumber) && E164Regex().IsMatch(phoneNumber);
    }
}
