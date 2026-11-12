using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Exchange.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Exchange.Responses;

/// <summary>
/// The last sales of an item. The client keeps it only when the person is its own.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct ExchangePriceHistoryResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Exchange;
    private const ExchangeOpcode Command = ExchangeOpcode.PriceHistory;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public int Person { get; init; }
    public int Item { get; init; }

    [WireArray(WireLength.Signed16)]
    public IReadOnlyList<ExchangePriceValue> Sales { get; init; }
}
