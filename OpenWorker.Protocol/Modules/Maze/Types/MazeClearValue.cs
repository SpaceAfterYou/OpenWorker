using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Maze.Types;

/// <summary>
/// What a person has cleared in a group of mazes: the episodes of one story that follow each other. The client always reads
/// <see cref="Slots"/> pairs, so the writer pads the list by hand.
/// </summary>
/// <param name="Group">The group the mazes belong to.</param>
/// <param name="OpenEpisode">How far the person can go in the group.</param>
/// <param name="Cleared">The mazes that are cleared, at most <see cref="Slots"/> of them, each with the number the client shows next to it.</param>
public readonly record struct MazeClearValue(int Group, byte OpenEpisode, IReadOnlyList<(int Maze, int Value)> Cleared) : IWireWritable
{
    /// <summary>
    /// The client has room for this many mazes in a group.
    /// </summary>
    public const int Slots = 10;

    public void Write(ref PacketWriter writer)
    {
        writer.WriteInt32(Group);
        writer.WriteByte(OpenEpisode);

        // The numbers go in pairs, one slot after another; the slots past the list are zero, the mazes past the slots are
        // left out.
        var cleared = Cleared ?? [];

        for (var slot = 0; slot < Slots; slot++)
        {
            var (maze, value) = slot < cleared.Count ? cleared[slot] : default;

            writer.WriteInt32(maze);
            writer.WriteInt32(value);
        }
    }
}
