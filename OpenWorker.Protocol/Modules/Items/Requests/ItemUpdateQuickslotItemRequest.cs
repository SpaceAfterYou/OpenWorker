using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Items.Requests;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct ItemUpdateQuickslotItemRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.UpdateQuickslotItem;

    [WireArray(Count = 2)]
    public int[] Items { get; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}