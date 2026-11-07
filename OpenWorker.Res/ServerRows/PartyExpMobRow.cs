using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// How much of the experience of a monster stays with a player, by how far the level of the player is above it.
/// </summary>
[BinaryResourceTable("tb_PartyExp_Mob", Optional = true)]
public readonly struct PartyExpMobRow(BinaryReader reader)
{
    public sbyte Interval { get; } = reader.ReadSByte();
    public float Rate { get; } = reader.ReadSingle();
}
