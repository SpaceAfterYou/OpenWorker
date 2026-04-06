using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Shop.Types;

public readonly struct CashInfoValue : IWritableData
{
#region Message: Body

    public required int Item { get; init; }
    public required short Count { get; init; }
    public required int BasePrice { get; init; }
    public required int Price { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Item);
        writer.Write(Count);
        writer.Write(BasePrice);
        writer.Write(Price);
    }

#endregion Interface: IWritableData
}
