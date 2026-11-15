using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Maze.Types;

/// <summary>
/// What a person has cleared in a group of mazes: the episodes of one story that follow each other. The client keeps the
/// group as it is told, so the lists are all the group has. The client always reads <see cref="Slots"/> pairs, so the writer
/// pads the lists by hand.
/// </summary>
/// <param name="Group">The group the mazes belong to.</param>
/// <param name="OpenEpisode">The last episode of the group open on the board: the episodes up to it are open, the first always.</param>
/// <param name="Normal">The mazes of the normal difficulty that are cleared; a hard maze opens once its normal one is here.</param>
/// <param name="Hard">The mazes of the hard difficulty that are cleared.</param>
public readonly record struct MazeClearValue(int Group, byte OpenEpisode, IReadOnlyList<int> Normal, IReadOnlyList<int> Hard) : IWireWritable
{
    /// <summary>
    /// The client has room for this many mazes of each difficulty in a group.
    /// </summary>
    public const int Slots = 10;

    public void Write(ref PacketWriter writer)
    {
        writer.WriteInt32(Group);
        writer.WriteByte(OpenEpisode);

        // A normal maze and a hard one in each slot; the slots past a list are zero, the mazes past the slots are left out.
        var normal = Normal ?? [];
        var hard = Hard ?? [];

        for (var slot = 0; slot < Slots; slot++)
        {
            writer.WriteInt32(slot < normal.Count ? normal[slot] : 0);
            writer.WriteInt32(slot < hard.Count ? hard[slot] : 0);
        }
    }
}
