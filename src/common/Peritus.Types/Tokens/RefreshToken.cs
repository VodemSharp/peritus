using System.ComponentModel;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Peritus.Primitives.Abstractions;
using Peritus.Primitives.Converters;

namespace Peritus.Types.Tokens;

[TypeConverter(typeof(StringValueTypeConverter<RefreshToken>))]
[JsonConverter(typeof(StringValueJsonConverter<RefreshToken>))]
public readonly record struct RefreshToken(string Value) : IStringValue
{
    public static implicit operator string(RefreshToken refreshToken) => refreshToken.Value;

    public static RefreshToken Generate()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return new RefreshToken(Convert.ToBase64String(bytes));
    }
}
