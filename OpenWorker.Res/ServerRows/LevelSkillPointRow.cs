using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// The skill points a level gives when it is reached, from the server's own <c>tb_Levelup_Point</c>. The client's table has
/// the experience of each level and not the points.
/// </summary>
[BinaryResourceTable("tb_Levelup_Point", Server = true)]
public readonly struct LevelSkillPointRow
{
    public LevelSkillPointRow(BinaryReader reader)
    {
        Id = reader.ReadByte();
        Level = reader.ReadByte();

        // The experience the level asks for: the client's table is the one read for it.
        reader.ReadInt32();

        SkillPoints = reader.ReadByte();

        // The most soul vapor a player of the level can hold.
        reader.ReadUInt16();
    }

    public byte Id { get; }

    public byte Level { get; }

    /// <summary>
    /// The points given to a player who reaches the level.
    /// </summary>
    public byte SkillPoints { get; }
}
