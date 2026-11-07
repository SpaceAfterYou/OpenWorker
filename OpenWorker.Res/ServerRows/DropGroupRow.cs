using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// Items of one drop group, one of them falls by the chance out of ten thousand written next to it.
/// </summary>
[BinaryResourceTable("tb_Drop_Group", Optional = true)]
public readonly struct DropGroupRow(BinaryReader reader)
{
    public const int ItemCount = 10;

    public int Id { get; } = reader.ReadInt32();
    public ushort[] Chance { get; } = Enumerable.Range(0, ItemCount).Select(_ => reader.ReadUInt16()).ToArray();
    public int[] Item { get; } = Enumerable.Range(0, ItemCount).Select(_ => reader.ReadInt32()).ToArray();
    public byte[] Min { get; } = Enumerable.Range(0, ItemCount).Select(_ => reader.ReadByte()).ToArray();
    public byte[] Max { get; } = Enumerable.Range(0, ItemCount).Select(_ => reader.ReadByte()).ToArray();
}
