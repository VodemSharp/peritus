using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Peritus.Primitives.Abstractions;

namespace Peritus.Persistence.ValueConverters;

public sealed class StringValueConverter<T>()
    : ValueConverter<T, string>(v => v.Value, v => _creator(v)) where T : IStringValue
{
    private static readonly Func<string, T> _creator = CreateCreator();

    private static Func<string, T> CreateCreator()
    {
        var constructor = typeof(T).GetConstructor([typeof(string)])
                          ?? throw new InvalidOperationException(
                              $"Type {typeof(T)} must have a constructor that accepts string");

        var param = Expression.Parameter(typeof(string), "value");
        var ctor = Expression.New(constructor, param);
        return Expression.Lambda<Func<string, T>>(ctor, param).Compile();
    }
}
