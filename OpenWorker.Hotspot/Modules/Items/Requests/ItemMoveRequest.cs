using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Items.Requests;

[HotspotMessage(Group, Command)]
public readonly struct ItemMoveRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.Move;

    public MoveItemValue Src { get; } = new(reader);
    public MoveItemValue Dest { get; } = new(reader);

    public MessageOpcode Opcode => new(Group, Command);
}