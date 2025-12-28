using Peritus.Persistence.Entities.Abstractions;

namespace Peritus.Persistence.Entities;

public class UpdatedEntity : IUpdatedEntity
{
    public DateTime UpdatedAt { get; set; }
}
