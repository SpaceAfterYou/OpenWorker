using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Extensions;
using OpenWorker.Hotspot.Modules.Items.Types;

namespace OpenWorker.Hotspot.Modules.Trade.Responses;

[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct TradeUpdateItemResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Trade;
    private const TradeOpcode Command = TradeOpcode.UpdateItem;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public required ActorValue Actor { get; init; }
    public required byte UpdateType { get; init; }
    public required ItemValue Item { get; init; }
    public required ItemSocketValue[] Sockets { get; init; }
    public required ItemBroachValue Broach { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Actor);
        writer.Write(UpdateType);
        writer.Write(Item);
        writer.Write(Sockets);
        Broach.Write(writer);
    }
}
