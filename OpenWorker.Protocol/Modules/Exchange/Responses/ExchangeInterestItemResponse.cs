using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Exchange.Responses;

/// <summary>
/// An item put on or taken off the list the player keeps an eye on. A result other than 0 is the message the client shows,
/// and the list stays as it was.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct ExchangeInterestItemResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Exchange;
    private const ExchangeOpcode Command = ExchangeOpcode.InterestItem;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public int Person { get; init; }
    public int Item { get; init; }
    public int Result { get; init; }
    public byte Add { get; init; }
}
