using System.ComponentModel;
using System.Text.Json.Serialization;
using Peritus.Primitives.Abstractions;
using Peritus.Primitives.Converters;

namespace Peritus.Types.Http;

[TypeConverter(typeof(StringValueTypeConverter<HttpClientName>))]
[JsonConverter(typeof(StringValueJsonConverter<HttpClientName>))]
public readonly record struct HttpClientName(string Value) : IStringValue
{
    public static HttpClientName IdentityApiRefresh => new("identity-api-refresh");

    public static implicit operator string(HttpClientName name) => name.Value;
}
