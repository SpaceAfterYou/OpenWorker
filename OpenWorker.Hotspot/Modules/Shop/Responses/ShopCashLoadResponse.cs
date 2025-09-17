using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Shop.Responses;

public readonly struct CashInfoValue
{
    public required int Item { get; init; }
    public required short Count { get; init; }
    public required int BasePrice { get; init; }
    public required int Price { get; init; }
}

public readonly struct CashItemValue
{
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
}

[HotspotMessage(Group, Command)]
public readonly struct ShopCashLoadResponse(IReadOnlyList<CashItemValue> itemList) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Shop;
    private const ShopOpcode Command = ShopOpcode.CashLoad;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(true); // idk
        writer.Write(itemList.Count);

        foreach (var item in itemList)
        {
            writer.Write((byte)item.CashInfoList.Count);

            writer.Write(item.Index);
            writer.Write(item.SellActive);
            writer.Write(item.Item);
            writer.Write(item.Level);
            writer.Write(item.Order);
            writer.Write(item.NeedSlot);
            writer.Write(item.CashInfo);
            writer.Write(item.LimitTime1);
            writer.Write(item.LimitTime2);
            writer.Write(item.SellCount);
            writer.Write(item.Billing);

            foreach (var info in item.CashInfoList)
            {
                writer.Write(info.Item);
                writer.Write(info.Count);
                writer.Write(info.BasePrice);
                writer.Write(info.Price);
            }
        }
    }
}