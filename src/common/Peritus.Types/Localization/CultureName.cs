using System.ComponentModel;
using System.Text.Json.Serialization;
using Peritus.Primitives.Abstractions;
using Peritus.Primitives.Converters;

namespace Peritus.Types.Localization;

[TypeConverter(typeof(StringValueTypeConverter<CultureName>))]
[JsonConverter(typeof(StringValueJsonConverter<CultureName>))]
public readonly record struct CultureName(string Value) : IStringValue
{
    public static implicit operator string(CultureName cultureName) => cultureName.Value;
}
