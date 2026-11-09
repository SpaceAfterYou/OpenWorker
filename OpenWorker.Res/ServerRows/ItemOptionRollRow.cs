using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// The options a new item can be given, from the server's own <c>tb_Random_Option</c>: how many it gets, from how many of the
/// listed options they are picked, and for each listed option its effect and the bounds of its value. The client's table of
/// the same name has the id and the bounds of the count only, so the item table names its row in both.
/// </summary>
[BinaryResourceTable("tb_Random_Option", Server = true)]
public readonly struct ItemOptionRollRow
{
    /// <summary>The options a row lists.</summary>
    public const int Options = 25;

    public ItemOptionRollRow(BinaryReader reader)
    {
        Id = reader.ReadInt32();
        Min = reader.ReadByte();
        Max = reader.ReadByte();
        Candidates = reader.ReadByte();

        Types = new ushort[Options];
        Lows = new int[Options];
        Highs = new int[Options];

        for (var i = 0; i < Options; i++)
        {
            Types[i] = reader.ReadUInt16();
        }

        for (var i = 0; i < Options; i++)
        {
            Lows[i] = reader.ReadInt32();
        }

        for (var i = 0; i < Options; i++)
        {
            Highs[i] = reader.ReadInt32();
        }

        // The row an option change of the item would roll from, which this client does not have.
        reader.ReadInt32();
    }

    public int Id { get; }

    /// <summary>The fewest options an item gets.</summary>
    public byte Min { get; }

    /// <summary>The most options an item gets.</summary>
    public byte Max { get; }

    /// <summary>How many of the listed options, from the first, the picks are made from.</summary>
    public byte Candidates { get; }

    /// <summary>The effect of each listed option, by the numbers of the stat effects.</summary>
    public ushort[] Types { get; }

    public int[] Lows { get; }

    public int[] Highs { get; }
}
