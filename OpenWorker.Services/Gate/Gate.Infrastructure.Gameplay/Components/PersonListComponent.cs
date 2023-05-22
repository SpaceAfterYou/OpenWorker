using DefaultEcs;

namespace OpenWorker.Services.Gate.Infrastructure.Gameplay.Components;

public sealed class PersonListComponent : List<Entity>
{
    public PersonListComponent(IEnumerable<Entity> entities) : base(entities) { }
}
