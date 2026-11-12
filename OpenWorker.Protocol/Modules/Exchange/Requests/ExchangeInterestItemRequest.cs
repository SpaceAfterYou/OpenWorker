using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Exchange.Requests;

/// <summary>
/// An item put on the list of the items the player keeps an eye on, or taken off it.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct ExchangeInterestItemRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Exchange;
    private const ExchangeOpcode Command = ExchangeOpcode.InterestItem;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public int Person { get; init; }
    public int Item { get; init; }

    /// <summary>
    /// 1 puts it on the list, 0 takes it off.
    /// </summary>
    public byte Add { get; init; }
}
