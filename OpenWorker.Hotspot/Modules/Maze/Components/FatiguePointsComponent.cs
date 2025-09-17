using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Hotspot.Modules.Maze.Components;

[EntityComponent(EntityComponentService.District)]
public readonly record struct FatiguePointsComponent
{
    public required short Common { get; init; }
    public required short Bonus { get; init; }
}