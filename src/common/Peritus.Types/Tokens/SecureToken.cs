using System.ComponentModel;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Peritus.Primitives.Abstractions;
using Peritus.Primitives.Converters;

namespace Peritus.Types.Tokens;

[TypeConverter(typeof(StringValueTypeConverter<SecureToken>))]
[JsonConverter(typeof(StringValueJsonConverter<SecureToken>))]
public readonly record struct SecureToken(string Value) : IStringValue
{
    public static implicit operator string(SecureToken token) => token.Value;

    public static SecureToken Generate(int byteLength = 32)
    {
        var bytes = new byte[byteLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return new SecureToken(Convert.ToHexString(bytes));
    }
}
