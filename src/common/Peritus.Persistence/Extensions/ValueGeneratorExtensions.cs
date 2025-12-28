using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Peritus.Persistence.ValueGenerators;
using Peritus.Primitives.Abstractions;

namespace Peritus.Persistence.Extensions;

public static class ValueGeneratorExtensions
{
    public static PropertyBuilder<TProperty> HasGuidValueGenerator<TProperty>(
        this PropertyBuilder<TProperty> builder)
        where TProperty : IGuidValue
    {
        return builder.HasValueGenerator<GuidValueGenerator<TProperty>>();
    }
}
