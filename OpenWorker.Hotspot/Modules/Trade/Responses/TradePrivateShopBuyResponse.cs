using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Trade.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct TradePrivateShopBuyResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Trade;
    private const TradeOpcode Command = TradeOpcode.PrivateShopBuy;

    public MessageOpcode Opcode => new(Group, Command);

    public required long Serial { get; init; }
    public required string Name { get; init; }
    public required int Item { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Serial);
        writer.WriteUtf16UnicodeString(Name, 32);
        writer.Write(Item);
    }
}
