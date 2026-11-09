using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// The share of the experience of its rank that a monster gives. The client table of monsters has no such column,
/// this table is made from the server table of monsters: an int for the monster and a float for the share.
/// </summary>
[BinaryResourceTable("tb_Monster_Reward", Optional = true)]
public readonly struct MonsterRewardRow(BinaryReader reader)
{
    public int Id { get; } = reader.ReadInt32();
    public float ExpRate { get; } = reader.ReadSingle();
}
