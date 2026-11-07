using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot.Modules.Boosters.Enums;

namespace OpenWorker.Gameplay.Modules.Boosters.Components;

/// <summary>
/// The boosters of the person, null until they are loaded.
/// </summary>
[EntityComponent(EntityComponentService.District)]
[EntityComponent(EntityComponentService.Maze)]
public struct BoosterComponent
{
    public int Person;

    public BoosterBook? Book;

    /// <summary>
    /// Where the person is, which decides whose time goes.
    /// </summary>
    public BoosterConsumeArea Area;

    /// <summary>
    /// When the time was taken from the boosters last.
    /// </summary>
    public DateTimeOffset LastTick;
}
