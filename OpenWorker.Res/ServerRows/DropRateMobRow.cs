using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// How much of the drop of a monster a player gets, by how far the level of the player is above it.
/// </summary>
[BinaryResourceTable("tb_DropRate_Mob", Optional = true)]
public readonly struct DropRateMobRow(BinaryReader reader)
{
    public byte Interval { get; } = reader.ReadByte();
    public float Rate { get; } = reader.ReadSingle();
}
