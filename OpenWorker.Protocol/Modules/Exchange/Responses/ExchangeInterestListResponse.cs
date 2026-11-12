using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Exchange.Responses;

/// <summary>
/// The items the player keeps an eye on. The client keeps it only when the person is its own.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct ExchangeInterestListResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Exchange;
    private const ExchangeOpcode Command = ExchangeOpcode.InterestList;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public int Person { get; init; }

    [WireArray(WireLength.Signed16)]
    public IReadOnlyList<int> Items { get; init; }
}
