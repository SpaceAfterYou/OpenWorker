using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// The server's own <c>tb_Skill</c>, of which only how much hate a blow of the skill earns is kept: the client's copy has no such
/// columns. The rest of the row is read to reach its end.
/// </summary>
[BinaryResourceTable("tb_Skill", Server = true)]
public readonly struct SkillAggroRow
{
    public SkillAggroRow(BinaryReader reader)
    {
        Id = reader.ReadUInt32();
        SkipString(reader);

        // Group, level and cost, then the times and ranges, up to the columns of the hate.
        reader.ReadBytes(139);

        Kind = reader.ReadByte();
        Value = reader.ReadUInt16();

        // The rank points, the four animations of the skill and the few small columns after them.
        reader.ReadBytes(2);
        SkipString(reader);
        SkipString(reader);
        SkipString(reader);
        SkipString(reader);
        reader.ReadBytes(14);
    }

    public uint Id { get; }

    /// <summary>Zero: the hate grows by <see cref="Value"/> a blow. Otherwise it grows by that many percent of the damage.</summary>
    public byte Kind { get; }

    public ushort Value { get; }

    private static void SkipString(BinaryReader reader)
    {
        var length = reader.ReadUInt16();

        reader.ReadBytes(length * sizeof(char));
    }
}
