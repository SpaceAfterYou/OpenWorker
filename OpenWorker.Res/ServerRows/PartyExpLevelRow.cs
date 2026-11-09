using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// How much of the experience of a monster a party gets, by how far apart the levels of its members are: the highest
/// level less the lowest.
/// </summary>
[BinaryResourceTable("tb_PartyExp_Level", Optional = true)]
public readonly struct PartyExpLevelRow(BinaryReader reader)
{
    public byte Interval { get; } = reader.ReadByte();
    public float Rate { get; } = reader.ReadSingle();
}
