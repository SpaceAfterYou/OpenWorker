using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// The server's own <c>tb_Skill</c>, of which only how much hate a blow of the skill earns and the skill a monster may go on with
/// are kept: the client's copy has no such columns. The rest of the row is read to reach its end.
/// </summary>
[BinaryResourceTable("tb_Skill", Server = true)]
public readonly struct SkillAggroRow
{
    public SkillAggroRow(BinaryReader reader)
    {
        Id = reader.ReadUInt32();
        SkipString(reader);

        // Group, level and cost, then the times, ranges and states, up to the next skill of a combo.
        reader.ReadBytes(116);

        ChainSkill = reader.ReadUInt32();
        ChainRate = reader.ReadByte();

        // The cooldowns and the bullets, up to the columns of the hate.
        reader.ReadBytes(18);

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

    /// <summary>The skill a monster goes on with after this one, when the chance comes up; zero for none.</summary>
    public uint ChainSkill { get; }

    /// <summary>The chance in percent that a monster goes on with <see cref="ChainSkill"/>.</summary>
    public byte ChainRate { get; }

    /// <summary>Zero: the hate grows by <see cref="Value"/> a blow. Otherwise it grows by that many percent of the damage.</summary>
    public byte Kind { get; }

    public ushort Value { get; }

    private static void SkipString(BinaryReader reader)
    {
        var length = reader.ReadUInt16();

        reader.ReadBytes(length * sizeof(char));
    }
}
