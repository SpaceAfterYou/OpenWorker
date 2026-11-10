using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Gameplay.Modules.Maze.Components;

/// <summary>
/// The entries of the person into the mazes today (<see cref="MazeEntryLimits"/>), null until they are loaded as the person
/// enters a district or a maze.
/// </summary>
[EntityComponent(EntityComponentService.District)]
public struct MazeEntryComponent
{
    public int Person;

    public MazeEntryBook? Book;
}
