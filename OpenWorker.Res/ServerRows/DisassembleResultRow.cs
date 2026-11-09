using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// What taking an item apart gives: an amount of ether between two bounds, and one of up to seven items, each with its
/// chance out of ten thousand and the bounds of its count. The client's <c>tb_Disassemble</c> has the ids only; the server
/// has its own table under the same name, which carries the results.
/// </summary>
[BinaryResourceTable("tb_Disassemble", Server = true)]
public readonly struct DisassembleResultRow
{
    /// <summary>The items a row can give.</summary>
    public const int Results = 7;

    public DisassembleResultRow(BinaryReader reader)
    {
        Id = reader.ReadInt32();
        MinEther = reader.ReadInt32();
        MaxEther = reader.ReadInt32();

        Items = new int[Results];

        for (var i = 0; i < Results; i++)
        {
            Items[i] = reader.ReadInt32();
        }

        Rates = new ushort[Results];

        for (var i = 0; i < Results; i++)
        {
            Rates[i] = reader.ReadUInt16();
        }

        MinCounts = reader.ReadBytes(Results);
        MaxCounts = reader.ReadBytes(Results);
    }

    public int Id { get; }

    public int MinEther { get; }

    public int MaxEther { get; }

    /// <summary>The items, zero where the row has none.</summary>
    public int[] Items { get; }

    /// <summary>The chance of each item out of ten thousand; one item comes out of a roll, or none.</summary>
    public ushort[] Rates { get; }

    public byte[] MinCounts { get; }

    public byte[] MaxCounts { get; }
}
