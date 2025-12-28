using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Peritus.Primitives.Abstractions;

namespace Peritus.Persistence.ValueConverters;

public sealed class GuidValueConverter<T>()
    : ValueConverter<T, Guid>(v => v.Value, v => _creator(v)) where T : IGuidValue
{
    private static readonly Func<Guid, T> _creator = CreateCreator();

    private static Func<Guid, T> CreateCreator()
    {
        var constructor = typeof(T).GetConstructor([typeof(Guid)])
                          ?? throw new InvalidOperationException(
                              $"Type {typeof(T)} must have a constructor that accepts Guid");

        var param = Expression.Parameter(typeof(Guid), "value");
        var ctor = Expression.New(constructor, param);
        return Expression.Lambda<Func<Guid, T>>(ctor, param).Compile();
    }
}
