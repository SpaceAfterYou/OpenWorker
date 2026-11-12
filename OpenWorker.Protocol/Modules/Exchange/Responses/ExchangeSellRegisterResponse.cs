using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Exchange.Responses;

/// <summary>
/// The result of a waiting listing put on sale: 0, or the message the client shows. The changed listing reaches the list of
/// the player by <see cref="ExchangeMyListResponse"/>.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct ExchangeSellRegisterResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Exchange;
    private const ExchangeOpcode Command = ExchangeOpcode.SellRegister;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public int Result { get; init; }
}
