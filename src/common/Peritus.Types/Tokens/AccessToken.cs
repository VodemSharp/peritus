using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.JsonWebTokens;
using Peritus.Primitives.Abstractions;
using Peritus.Primitives.Converters;

namespace Peritus.Types.Tokens;

[TypeConverter(typeof(StringValueTypeConverter<AccessToken>))]
[JsonConverter(typeof(StringValueJsonConverter<AccessToken>))]
public readonly record struct AccessToken(string Value) : IStringValue
{
    public static implicit operator string(AccessToken accessToken) => accessToken.Value;

    public DateTimeOffset? GetExpiration()
    {
        try
        {
            var token = new JsonWebTokenHandler().ReadJsonWebToken(Value);
            var exp = token.GetPayloadValue<long>(JwtRegisteredClaimNames.Exp);
            return DateTimeOffset.FromUnixTimeSeconds(exp);
        }
        catch
        {
            return null;
        }
    }
}
