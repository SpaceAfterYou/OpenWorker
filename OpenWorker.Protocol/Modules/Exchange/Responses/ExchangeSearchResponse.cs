using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Exchange.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Exchange.Responses;

/// <summary>
/// A page of the listings that matched a search, and how many pages there are.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct ExchangeSearchResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Exchange;
    private const ExchangeOpcode Command = ExchangeOpcode.Search;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public required int Page { get; init; }
    public required int PageMax { get; init; }
    [WireArray(WireLength.Signed16)]
    public required IReadOnlyList<ExchangeItemValue> Items { get; init; }
}
