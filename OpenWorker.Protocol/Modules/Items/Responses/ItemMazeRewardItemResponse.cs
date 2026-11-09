using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Items.Responses;

/// <summary>
/// The key reward of the window at the end of a maze has been taken. The client reads nothing of it; the items that came and
/// went are told by the messages of the inventory.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct ItemMazeRewardItemResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.MazeRewardItem;

    public MessageOpcode Opcode => new(Group, (byte)Command);
}