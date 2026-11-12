using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Exchange.Types;

namespace OpenWorker.Hotspot.Modules.Exchange.Responses;

/// <summary>
/// Listings of the player: the whole list at the entry, or the ones that changed (a sale, a new listing).
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct ExchangeMyListResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Exchange;
    private const ExchangeOpcode Command = ExchangeOpcode.MyList;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public required IReadOnlyList<ExchangeMyItemValue> Items { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(checked((short)Items.Count));

        foreach (var item in Items)
        {
            item.Write(writer);
        }
    }
}
