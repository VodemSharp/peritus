using Peritus.Persistence.Entities.Abstractions;

namespace Peritus.Persistence.Entities;

public abstract class TimestampedEntity : ITimestamped
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
