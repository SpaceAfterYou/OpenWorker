using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Exchange.Responses;

/// <summary>
/// The result of a purchase: 0 takes the count bought off the listing the search window shows, anything else is the
/// message the client shows.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct ExchangeItemBuyResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Exchange;
    private const ExchangeOpcode Command = ExchangeOpcode.ItemBuy;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public int Result { get; init; }
    public int Exchange { get; init; }
    public short Count { get; init; }
}
