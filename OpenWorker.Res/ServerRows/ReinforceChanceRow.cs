using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// The chances of an upgrade, out of ten thousand, for each level the item is at: that it goes up, that it stays and that it
/// breaks. The server has its own <c>tb_Reinforce</c>, which carries them; the client's has the costs only, and the costs are
/// read from the client's table so that the player pays what the client shows.
/// </summary>
[BinaryResourceTable("tb_Reinforce", Server = true)]
public readonly struct ReinforceChanceRow
{
    /// <summary>The levels a row has chances for: from 0 to 9, the level the item is at before the upgrade.</summary>
    public const int Levels = 10;

    // The limits and the costs come first, as in the client's table: three limits, then ether, gold, battle points and the
    // first material for each level, its count, the second material and its count.
    private const int Costs = 3 * sizeof(short) + 4 * Levels * sizeof(int) + Levels * sizeof(short) + Levels * sizeof(int) + Levels * sizeof(short);

    public ReinforceChanceRow(BinaryReader reader)
    {
        Id = reader.ReadInt32();
        reader.ReadBytes(Costs);

        Stay = Read(reader);
        Up = Read(reader);
        Break = Read(reader);

        // The stats each level gives.
        reader.ReadBytes(Levels * sizeof(float));
    }

    public int Id { get; }

    public ushort[] Stay { get; }

    public ushort[] Up { get; }

    public ushort[] Break { get; }

    private static ushort[] Read(BinaryReader reader)
    {
        var values = new ushort[Levels];

        for (var i = 0; i < values.Length; i++)
        {
            values[i] = reader.ReadUInt16();
        }

        return values;
    }
}
