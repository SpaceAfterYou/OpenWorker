using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Gameplay.Modules.Maze.Components;

/// <summary>
/// The fatigue of the person (<see cref="OpenWorker.Gameplay.Modules.Maze.FatigueService"/>): the points of the day, the points saved up from the
/// days before, and when they were last filled up for a new game day.
/// </summary>
[EntityComponent(EntityComponentService.District)]
public readonly record struct FatiguePointsComponent
{
    public required short Common { get; init; }
    public required short Bonus { get; init; }

    /// <summary>
    /// When the points were last filled up; null when they never were.
    /// </summary>
    public DateTime? RefilledAt { get; init; }
}
