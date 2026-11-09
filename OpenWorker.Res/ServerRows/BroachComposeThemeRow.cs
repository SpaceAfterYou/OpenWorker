using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// The chances of the theme of a composed broach, out of ten thousand, by the themes of the five broaches put in: each counts
/// 1, 10, 100 or 1000 for theme 1 to 4, and the row is the sum. Not in the client files, the server tables have it.
/// </summary>
[BinaryResourceTable("TB_BroachCompose_Theme", Optional = true)]
public readonly struct BroachComposeThemeRow
{
    /// <summary>The themes a composed broach may come out with.</summary>
    public const int Themes = 4;

    public BroachComposeThemeRow(BinaryReader reader)
    {
        Sum = reader.ReadUInt16();
        Chances = Enumerable.Range(0, Themes).Select(_ => reader.ReadUInt16()).ToArray();
    }

    /// <summary>The sum of the themes of the broaches put in.</summary>
    public ushort Sum { get; }

    /// <summary>The chance of each theme of the result, from the first.</summary>
    public ushort[] Chances { get; }
}
