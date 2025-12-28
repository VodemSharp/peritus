using System.ComponentModel;
using System.Globalization;
using Peritus.Primitives.Abstractions;

namespace Peritus.Primitives.Converters;

public sealed class StringValueTypeConverter<T> : TypeConverter where T : struct, IStringValue
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string)
               || base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string s)
        {
            return (T)Activator.CreateInstance(typeof(T), s)!;
        }

        return base.ConvertFrom(context, culture, value);
    }

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType == typeof(string)
               || base.CanConvertTo(context, destinationType);
    }

    public override object? ConvertTo(
        ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is T t)
        {
            return t.Value;
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }
}
