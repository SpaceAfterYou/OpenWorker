using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Gameplay.Modules.Tower.Components;

/// <summary>
/// The climb of the person up the infinite tower (<see cref="InfiniteTower"/>), null until it is loaded as the person enters a
/// district or a maze.
/// </summary>
[EntityComponent(EntityComponentService.District)]
public struct InfiniteTowerComponent
{
    public int Person;

    public InfiniteTowerBook? Book;
}
