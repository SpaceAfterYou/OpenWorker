using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Exchange.Requests;

/// <summary>
/// Some of a listing bought, at the price it is listed for.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct ExchangeItemBuyRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Exchange;
    private const ExchangeOpcode Command = ExchangeOpcode.ItemBuy;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public int Exchange { get; init; }
    public int Item { get; init; }
    public short Count { get; init; }

    /// <summary>
    /// The mark of the listing as the search showed it: sold without commission.
    /// </summary>
    public byte NoCommission { get; init; }
}
