using System.ComponentModel;
using System.Text.Json.Serialization;
using Peritus.Primitives.Abstractions;
using Peritus.Primitives.Converters;

namespace Peritus.Types.Tokens;

[TypeConverter(typeof(GuidValueTypeConverter<AccessTokenId>))]
[JsonConverter(typeof(GuidValueJsonConverter<AccessTokenId>))]
public readonly record struct AccessTokenId(Guid Value) : IGuidValue
{
    public static implicit operator Guid(AccessTokenId accessTokenId) => accessTokenId.Value;

    public static AccessTokenId Create()
    {
        return new AccessTokenId(Guid.CreateVersion7());
    }
}
