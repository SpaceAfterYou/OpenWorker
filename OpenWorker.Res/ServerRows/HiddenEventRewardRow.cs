using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// The reward of a hidden event the server pays. The client's <c>tb_Hidden_Event</c> has the same columns with a placeholder
/// item; the server has its own table under the same name, which carries the items paid.
/// </summary>
[BinaryResourceTable("tb_Hidden_Event", Server = true)]
public readonly struct HiddenEventRewardRow
{
    private const int Conditions = 5;

    public HiddenEventRewardRow(BinaryReader reader)
    {
        Id = reader.ReadUInt16();

        // The conditions and their weights are the client's: the window shows only the conditions it knows.
        reader.ReadBytes(Conditions * (sizeof(uint) + sizeof(ushort)));

        Item = reader.ReadInt32();
        Count = reader.ReadByte();
    }

    public ushort Id { get; }

    public int Item { get; }

    public byte Count { get; }
}
