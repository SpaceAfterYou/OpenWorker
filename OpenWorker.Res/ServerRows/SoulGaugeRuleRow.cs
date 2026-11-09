using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// When the soul gauge of a hero fills by itself, from the server-only <c>tb_Soul_Guage</c>; the row of a hero is the one
/// of its number.
/// </summary>
[BinaryResourceTable("tb_Soul_Guage", Optional = true)]
public readonly struct SoulGaugeRuleRow
{
    public SoulGaugeRuleRow(BinaryReader reader)
    {
        Hero = reader.ReadByte();
        Regeneration = reader.ReadByte();
        Delay = reader.ReadUInt32();

        // Whether the gauge empties, and a buff the hero gets past a level of the gauge: its condition, the level and the buff.
        reader.ReadByte();
        reader.ReadByte();
        reader.ReadUInt16();
        reader.ReadUInt16();
    }

    public byte Hero { get; }

    /// <summary>
    /// 3 fills at every moment; 5 waits <see cref="Delay"/> after a skill before it fills again. The other kinds wait for a
    /// buff, which no hero of the table uses.
    /// </summary>
    public byte Regeneration { get; }

    /// <summary>
    /// In milliseconds.
    /// </summary>
    public uint Delay { get; }
}
