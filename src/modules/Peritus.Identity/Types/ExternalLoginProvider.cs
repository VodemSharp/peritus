using System.ComponentModel;
using System.Text.Json.Serialization;
using Peritus.Primitives.Abstractions;
using Peritus.Primitives.Converters;

namespace Peritus.Identity.Types;

[TypeConverter(typeof(StringValueTypeConverter<ExternalLoginProvider>))]
[JsonConverter(typeof(StringValueJsonConverter<ExternalLoginProvider>))]
public readonly record struct ExternalLoginProvider(string Value) : IStringValue
{
    public static ExternalLoginProvider Google => new("Google");
    public static implicit operator string(ExternalLoginProvider externalLoginProvider) => externalLoginProvider.Value;
}
