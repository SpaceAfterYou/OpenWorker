using OpenWorker.Extensions;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Shop.Types;

public readonly struct CashItemValue : IWritableData
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

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write((byte)CashInfoList.Count);

        writer.Write(Index);
        writer.Write(SellActive);
        writer.Write(Item);
        writer.Write(Level);
        writer.Write(Order);
        writer.Write(NeedSlot);
        writer.Write(CashInfo);
        writer.Write(LimitTime1);
        writer.Write(LimitTime2);
        writer.Write(SellCount);
        writer.Write(Billing);

        foreach (var info in CashInfoList)
        {
            info.Write(writer);
        }
    }

#endregion Interface: IWritableData
}
