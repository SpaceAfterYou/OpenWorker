using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Gameplay.Modules.DailyMissions.Components;

/// <summary>
/// The daily missions of the person, null until they are loaded.
/// </summary>
[EntityComponent(EntityComponentService.District)]
[EntityComponent(EntityComponentService.Maze)]
public struct DailyMissionComponent
{
    public int Person;

    public DailyMissionBook? Book;
}
