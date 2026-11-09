using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// The progress of a district or of a group of mazes, from the server's own <c>tb_DS_Point</c>: how many points a cleared
/// maze adds, the district a group belongs to, the points of each reward and the rewards. The client's table has the same
/// rows without the district of a group and without the points of a clear.
/// </summary>
[BinaryResourceTable("tb_DS_Point", Server = true)]
public readonly struct SharePointRow
{
    /// <summary>The steps of the reward of a group of mazes; a district has one.</summary>
    public const int Steps = 5;

    public SharePointRow(BinaryReader reader)
    {
        Id = reader.ReadUInt16();
        Kind = reader.ReadByte();
        District = reader.ReadUInt16();

        // The points a clear adds: the first one for a maze of no episode, then one for each episode from the first.
        ClearPoints = reader.ReadBytes(1 + Steps - 1);

        // The fifth episode has a value of its own that nothing reads: an episode past the fourth counts as the fourth.
        reader.ReadByte();

        Points = new ushort[Steps];

        for (var i = 0; i < Steps; i++)
        {
            Points[i] = reader.ReadUInt16();
        }

        ItemKinds = reader.ReadBytes(Steps);

        Items = new int[Steps];

        for (var i = 0; i < Steps; i++)
        {
            Items[i] = reader.ReadInt32();
        }

        ItemCounts = reader.ReadBytes(Steps);
    }

    public ushort Id { get; }

    /// <summary>
    /// 0 for a district, 1 for a group of mazes.
    /// </summary>
    public byte Kind { get; }

    /// <summary>
    /// The district of a group of mazes (its row in this table); 0 for a district.
    /// </summary>
    public ushort District { get; }

    /// <summary>
    /// The points a cleared maze of the group adds, by the episode of the maze (from 0 to 4).
    /// </summary>
    public byte[] ClearPoints { get; }

    /// <summary>
    /// The points each step of the reward asks for; the last one is also the most a group can have.
    /// </summary>
    public ushort[] Points { get; }

    /// <summary>
    /// What the item of a step is: 2 for a row of <c>tb_Quest_Reward</c>, which has an item for each class; else the item
    /// itself.
    /// </summary>
    public byte[] ItemKinds { get; }

    public int[] Items { get; }

    public byte[] ItemCounts { get; }
}
