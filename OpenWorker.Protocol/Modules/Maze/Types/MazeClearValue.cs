namespace OpenWorker.Hotspot.Modules.Maze.Types;

/// <summary>
/// What a person has cleared in a group of mazes: the episodes of one story that follow each other.
/// </summary>
/// <param name="Group">The group the mazes belong to.</param>
/// <param name="OpenEpisode">How far the person can go in the group.</param>
/// <param name="Cleared">The mazes that are cleared, at most <see cref="Slots"/> of them, each with the number the client shows next to it.</param>
public readonly record struct MazeClearValue(int Group, byte OpenEpisode, IReadOnlyList<(int Maze, int Value)> Cleared)
{
    /// <summary>
    /// The client has room for this many mazes in a group.
    /// </summary>
    public const int Slots = 10;
}
