using Peritus.Persistence.Entities.Abstractions;

namespace Peritus.Persistence.Entities;

public class CreatedEntity : ICreatedEntity
{
    public DateTime CreatedAt { get; set; }
}
