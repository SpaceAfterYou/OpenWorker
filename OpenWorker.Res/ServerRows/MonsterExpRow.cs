using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// What a monster of a level gives, for each rank. Not in the client files, the server tables have it.
/// </summary>
[BinaryResourceTable("tb_Monster_EXP", Optional = true)]
public readonly struct MonsterExpRow(BinaryReader reader)
{
    public byte Id { get; } = reader.ReadByte();
    public byte Level { get; } = reader.ReadByte();

    /// <summary>
    /// Slave, normal, elite, named, boss, raid and summoned, by rank.
    /// </summary>
    public int[] ByRank { get; } = Enumerable.Range(0, 7).Select(_ => reader.ReadInt32()).ToArray();
}
