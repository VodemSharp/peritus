using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using Peritus.Primitives.Abstractions;

namespace Peritus.Persistence.ValueGenerators;

public sealed class GuidValueGenerator<T> : ValueGenerator<T>
    where T : IGuidValue
{
    private static readonly Func<Guid, T> _creator = CreateCreator();

    public override bool GeneratesTemporaryValues => false;

    public override T Next(EntityEntry entry)
    {
        return _creator(Guid.CreateVersion7());
    }

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
