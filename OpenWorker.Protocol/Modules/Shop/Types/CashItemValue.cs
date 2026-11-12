using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Shop.Types;

/// <summary>
/// An item of the cash shop. The count of its prices goes first and the prices last, so the writer is written by hand.
/// </summary>
public readonly struct CashItemValue : IWireWritable
{
#region Message: Body

    public required int Index { get; init; }
    public required byte SellActive { get; init; }
    public required int Item { get; init; }
    public required short Level { get; init; }
    public required int Order { get; init; }
    public required byte NeedSlot { get; init; }
    public required short CashInfo { get; init; }
    public required DateTimeOffset LimitTime1 { get; init; }
    public required DateTimeOffset LimitTime2 { get; init; }
    public required byte SellCount { get; init; }
    public required int Billing { get; init; }
    public required List<CashInfoValue> CashInfoList { get; init; }

#endregion Message: Body

#region Interface: IWireWritable

    public void Write(ref PacketWriter writer)
    {
        var prices = CashInfoList ?? [];

        writer.WriteLength(WireLength.Byte, prices.Count);

        writer.WriteInt32(Index);
        writer.WriteByte(SellActive);
        writer.WriteInt32(Item);
        writer.WriteInt16(Level);
        writer.WriteInt32(Order);
        writer.WriteByte(NeedSlot);
        writer.WriteInt16(CashInfo);
        writer.WriteDateTimeOffset(LimitTime1);
        writer.WriteDateTimeOffset(LimitTime2);
        writer.WriteByte(SellCount);
        writer.WriteInt32(Billing);

        foreach (var price in prices)
        {
            price.Write(ref writer);
        }
    }

#endregion Interface: IWireWritable
}
