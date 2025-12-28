using System.ComponentModel;
using System.Globalization;
using Peritus.Primitives.Abstractions;

namespace Peritus.Primitives.Converters;

public sealed class GuidValueTypeConverter<T> : TypeConverter where T : struct, IGuidValue
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string)
               || sourceType == typeof(Guid)
               || base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        return value switch
        {
            string s when Guid.TryParse(s, out var guid) => (T)Activator.CreateInstance(typeof(T), guid)!,
            Guid g => (T)Activator.CreateInstance(typeof(T), g)!,
            _ => base.ConvertFrom(context, culture, value)
        };
    }

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType == typeof(string)
               || destinationType == typeof(Guid)
               || base.CanConvertTo(context, destinationType);
    }

    public override object? ConvertTo(
        ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (value is not T t)
        {
            return base.ConvertTo(context, culture, value, destinationType);
        }

        if (destinationType == typeof(string))
        {
            return t.Value.ToString();
        }

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (destinationType == typeof(Guid))
        {
            return t.Value;
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }
}
