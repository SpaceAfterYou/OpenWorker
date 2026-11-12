using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Exchange.Requests;

/// <summary>
/// A waiting listing put on sale at a price for one, with the tickets the player chose to use: one that takes the
/// commission away, one that allows more listings and one that makes the sale run longer.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct ExchangeSellRegisterRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Exchange;
    private const ExchangeOpcode Command = ExchangeOpcode.SellRegister;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    /// <summary>
    /// The serial of the listing, as the list of the player shows it.
    /// </summary>
    public long Serial { get; init; }

    public long Price { get; init; }
    public byte CommissionTicket { get; init; }
    public byte CountTicket { get; init; }
    public byte ExpireTicket { get; init; }
}
