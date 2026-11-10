using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

// The tables the server reads to make the monsters of a maze stronger for a party. The client has a smaller copy of the
// first one, and none of the monster's group.

/// <summary>
/// The server's own <c>tb_Monster</c>, of which only what the strength of a monster needs is kept: its level, its kind and
/// the group of <see cref="PartyReviseRatesRow"/> it belongs to. The rest of the row is read to reach its end.
/// </summary>
[BinaryResourceTable("tb_Monster", Server = true)]
public readonly struct MonsterReviseGroupRow
{
    public MonsterReviseGroupRow(BinaryReader reader)
    {
        Id = reader.ReadUInt32();
        SkipString(reader);
        SkipString(reader);
        reader.ReadBytes(42);
        Level = reader.ReadByte();
        reader.ReadBytes(9);
        Type = reader.ReadByte();
        reader.ReadBytes(3);
        SkipString(reader);
        SkipString(reader);
        reader.ReadBytes(47);
        SkipString(reader);
        reader.ReadBytes(2);
        SkipString(reader);
        reader.ReadBytes(8);
        SkipString(reader);
        SkipString(reader);
        SkipString(reader);
        reader.ReadBytes(3);
        SkipString(reader);
        SkipString(reader);
        SkipString(reader);
        reader.ReadBytes(18);
        SkipString(reader);
        SkipString(reader);
        SkipString(reader);
        reader.ReadBytes(62);
        PartyRevise = reader.ReadUInt32();
        reader.ReadBytes(186);
    }

    public uint Id { get; }

    public byte Level { get; }

    /// <summary>The kind of the monster; a monster that is not of the plain kind (0) is made as strong as for one player.</summary>
    public byte Type { get; }

    /// <summary>The group of rows of <see cref="PartyReviseRatesRow"/> that says how much stronger the monster is for a party.</summary>
    public uint PartyRevise { get; }

    private static void SkipString(BinaryReader reader)
    {
        var length = reader.ReadUInt16();

        reader.ReadBytes(length * sizeof(char));
    }
}

/// <summary>
/// How much stronger a monster of a group is for a party of some members: a row per group and number of members (one to
/// four). The rates multiply the life, the attacks and the defences of the monster, its attack speed and its speed of
/// movement; the gauge of its super armor is a percent of its life, and the times are milliseconds.
/// </summary>
[BinaryResourceTable("tb_PartyRevise", Server = true)]
public readonly struct PartyReviseRatesRow(BinaryReader reader)
{
    public uint Id { get; } = reader.ReadUInt32();
    public uint Group { get; } = reader.ReadUInt32();
    public byte MemberCount { get; } = reader.ReadByte();
    public float HealthRate { get; } = reader.ReadSingle();
    public float AttackRate { get; } = reader.ReadSingle();
    public float DefenseRate { get; } = reader.ReadSingle();
    public float AttackSpeedRate { get; } = reader.ReadSingle();
    public float MoveSpeedRate { get; } = reader.ReadSingle();
    public float SuperArmorGauge { get; } = reader.ReadSingle();
    public uint SuperArmorRegenTime { get; } = reader.ReadUInt32();
    public uint SuperArmorRecoverTime { get; } = reader.ReadUInt32();
}

/// <summary>
/// How much of the drop of a monster a party gets, by how far apart the levels of its members are: the highest level less
/// the lowest.
/// </summary>
[BinaryResourceTable("tb_DropRate_Level", Optional = true)]
public readonly struct DropRateLevelRow(BinaryReader reader)
{
    public byte Interval { get; } = reader.ReadByte();
    public float Rate { get; } = reader.ReadSingle();
}

/// <summary>
/// How much of the drop of a monster each member of a party gets, by how many members the party has in the maze.
/// </summary>
[BinaryResourceTable("tb_DropRate_Member", Optional = true)]
public readonly struct DropRateMemberRow(BinaryReader reader)
{
    public byte Members { get; } = reader.ReadByte();
    public float Rate { get; } = reader.ReadSingle();
}
