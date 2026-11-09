using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// The broaches a compose may give. The server has its own <c>TB_BroachData</c>: its index is theme × 100000 + kind × 10000
/// + rank × 1000 + a number from 1, and the first row of each group says how many the group has. The client's table of the
/// same name has the index and the item only, and the first theme only.
/// </summary>
[BinaryResourceTable("TB_BroachData", Server = true)]
public readonly struct BroachComposeItemRow(BinaryReader reader)
{
    public int Index { get; } = reader.ReadInt32();

    /// <summary>The broach item.</summary>
    public int Item { get; } = reader.ReadInt32();

    /// <summary>The kind, 1 to 3: attack, defence, utility.</summary>
    public byte Kind { get; } = reader.ReadByte();

    /// <summary>The rank in the compose, 1 to 5: the rank of the item less one.</summary>
    public byte Rank { get; } = reader.ReadByte();

    /// <summary>How many broaches the group of this row has, on its first row; 0 on the others.</summary>
    public byte GroupSize { get; } = reader.ReadByte();
}
