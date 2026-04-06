using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Items.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct ItemUpdateQuickslotItemResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.UpdateQuickslotItem;

    public MessageOpcode Opcode => new(Group, Command);

    public void Write(BinaryWriter writer)
    {
    }
}