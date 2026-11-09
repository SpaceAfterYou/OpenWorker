using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// The chances of the rank of a composed broach, out of ten thousand, by the ranks of the five broaches put in: each counts
/// 2, 4, 6, 8 or 10 for an item of rank 2 to 6, and the row is the sum. Not in the client files, the server tables have it.
/// </summary>
[BinaryResourceTable("TB_BroachCompose_Rank", Optional = true)]
public readonly struct BroachComposeRankRow
{
    /// <summary>The ranks a composed broach may come out with.</summary>
    public const int Ranks = 5;

    public BroachComposeRankRow(BinaryReader reader)
    {
        Sum = reader.ReadUInt16();
        Chances = Enumerable.Range(0, Ranks).Select(_ => reader.ReadUInt16()).ToArray();
    }

    /// <summary>The sum of the ranks of the broaches put in.</summary>
    public ushort Sum { get; }

    /// <summary>The chance of each rank of the result, from the first.</summary>
    public ushort[] Chances { get; }
}
