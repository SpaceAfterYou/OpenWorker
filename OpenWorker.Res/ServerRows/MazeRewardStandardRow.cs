using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// How a run of a maze is scored, by maze: the points of the clear itself, of the size of the party, of killing enough
/// monsters, and the points each rank starts above. Not in the client files, the server tables have it.
/// </summary>
[BinaryResourceTable("tb_MazeReward_Standard", Optional = true)]
public readonly struct MazeRewardStandardRow
{
    /// <summary>A party has at most four members, and each size has its points.</summary>
    public const int PartySizes = 4;

    /// <summary>The ranks above the lowest, from C to SS, each with the points it starts above.</summary>
    public const int RankSteps = 5;

    public MazeRewardStandardRow(BinaryReader reader)
    {
        Maze = reader.ReadInt16();
        ClearPoint = reader.ReadUInt16();
        Kills = reader.ReadInt16();
        KillsReached = reader.ReadInt16();
        KillsMissed = reader.ReadInt16();

        PartyPoints = new short[PartySizes];

        for (var i = 0; i < PartySizes; i++)
        {
            PartyPoints[i] = reader.ReadInt16();
        }

        RankPoints = new int[RankSteps];

        for (var i = 0; i < RankSteps; i++)
        {
            RankPoints[i] = reader.ReadInt32();
        }
    }

    public short Maze { get; }

    /// <summary>
    /// The points every clear starts with.
    /// </summary>
    public ushort ClearPoint { get; }

    /// <summary>
    /// The kills a run needs for <see cref="KillsReached"/>; fewer give <see cref="KillsMissed"/>.
    /// </summary>
    public short Kills { get; }

    public short KillsReached { get; }

    public short KillsMissed { get; }

    /// <summary>
    /// The points of a party of one to four, by size less one.
    /// </summary>
    public short[] PartyPoints { get; }

    /// <summary>
    /// The highest score of each rank from F to S: a score at or below the first is F, above the last is SS.
    /// </summary>
    public int[] RankPoints { get; }
}
