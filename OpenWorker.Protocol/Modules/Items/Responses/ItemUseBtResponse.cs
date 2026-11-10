using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Items.Responses;

/// <summary>
/// A player used an item: the player, the item and the slot of the bag it was in. Whoever sees the player plays the motion of
/// the item; the player's own client starts the cooldown of the slot.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct ItemUseBtResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.UseBt;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public int Actor { get; init; }

    public int Item { get; init; }

    public short Slot { get; init; }
}
