using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Peritus.Persistence.ValueConverters;
using Peritus.Primitives.Abstractions;

namespace Peritus.Persistence.Extensions;

public static class ConversionExtensions
{
    extension(ModelConfigurationBuilder builder)
    {
        public PropertiesConfigurationBuilder<T> ConfigureGuidValue<T>()
            where T : struct, IGuidValue
        {
            return builder.Properties<T>().HaveConversion<GuidValueConverter<T>>();
        }

        public PropertiesConfigurationBuilder<T> ConfigureStringValue<T>()
            where T : struct, IStringValue
        {
            return builder.Properties<T>().HaveConversion<StringValueConverter<T>>();
        }
    }
}
