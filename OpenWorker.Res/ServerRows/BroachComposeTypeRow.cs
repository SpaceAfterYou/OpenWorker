using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// The chances of the kind of a composed broach (attack, defence, utility), out of ten thousand, by the kinds of the five
/// broaches put in: each counts 1, 10 or 100, and the row is the sum. Not in the client files, the server tables have it.
/// </summary>
[BinaryResourceTable("TB_BroachCompose_Type", Optional = true)]
public readonly struct BroachComposeTypeRow
{
    /// <summary>The kinds a composed broach may come out as.</summary>
    public const int Kinds = 3;

    public BroachComposeTypeRow(BinaryReader reader)
    {
        Sum = reader.ReadUInt16();
        Chances = Enumerable.Range(0, Kinds).Select(_ => reader.ReadUInt16()).ToArray();
    }

    /// <summary>The sum of the kinds of the broaches put in.</summary>
    public ushort Sum { get; }

    /// <summary>The chance of each kind of the result: attack, defence, utility.</summary>
    public ushort[] Chances { get; }
}
