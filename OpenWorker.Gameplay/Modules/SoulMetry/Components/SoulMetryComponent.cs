using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Gameplay.Modules.SoulMetry.Components;

/// <summary>
/// The soul metries of the person, null until they are loaded.
/// </summary>
[EntityComponent(EntityComponentService.District)]
[EntityComponent(EntityComponentService.Maze)]
public struct SoulMetryComponent
{
    public int Person;

    public SoulMetryState? State;
}
