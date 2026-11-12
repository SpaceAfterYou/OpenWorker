using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// The kinds of items as the server's own <c>tb_Item_Classify</c> has them: besides the groups, the gold the exchange takes
/// for putting an item of the kind on sale. The client's table under the same name has other columns and no fee.
/// </summary>
[BinaryResourceTable("tb_Item_Classify", Server = true)]
public readonly struct ItemClassifyFeeRow(BinaryReader reader)
{
    public int Id { get; } = reader.ReadInt32();

    public byte Group { get; } = reader.ReadByte();
    public byte SubGroup { get; } = reader.ReadByte();
    public byte Category { get; } = reader.ReadByte();
    public byte SubCategory { get; } = reader.ReadByte();
    public byte Gain { get; } = reader.ReadByte();
    public byte Storage { get; } = reader.ReadByte();
    public byte SlotType { get; } = reader.ReadByte();
    public byte RepairType { get; } = reader.ReadByte();
    public byte UseState { get; } = reader.ReadByte();
    public byte UseType { get; } = reader.ReadByte();
    public byte Consume { get; } = reader.ReadByte();

    /// <summary>
    /// The gold the exchange takes when an item of the kind goes on sale.
    /// </summary>
    public ushort ExchangeFee { get; } = reader.ReadUInt16();

    public short Align { get; } = reader.ReadInt16();
}
