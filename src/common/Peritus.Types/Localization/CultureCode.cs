using System.ComponentModel;
using System.Text.Json.Serialization;
using Peritus.Primitives.Abstractions;
using Peritus.Primitives.Converters;

namespace Peritus.Types.Localization;

[TypeConverter(typeof(StringValueTypeConverter<CultureCode>))]
[JsonConverter(typeof(StringValueJsonConverter<CultureCode>))]
public readonly record struct CultureCode(string Value) : IStringValue
{
    public static implicit operator string(CultureCode cultureCode) => cultureCode.Value;
}
