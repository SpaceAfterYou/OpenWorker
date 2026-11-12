using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Exchange.Types;

namespace OpenWorker.Hotspot.Modules.Exchange.Responses;

/// <summary>
/// A page of the listings that matched a search, and how many pages there are.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct ExchangeSearchResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Exchange;
    private const ExchangeOpcode Command = ExchangeOpcode.Search;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public required int Page { get; init; }
    public required int PageMax { get; init; }
    public required IReadOnlyList<ExchangeItemValue> Items { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Page);
        writer.Write(PageMax);
        writer.Write(checked((short)Items.Count));

        foreach (var item in Items)
        {
            item.Write(writer);
        }
    }
}
