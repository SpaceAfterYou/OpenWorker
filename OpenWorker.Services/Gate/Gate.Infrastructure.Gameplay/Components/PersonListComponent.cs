using DefaultEcs;

namespace Gate.Infrastructure.Gameplay.Components;

public sealed class PersonListComponent : List<Entity> 
{
    public PersonListComponent(IEnumerable<Entity> entities) : base(entities) { }
}
