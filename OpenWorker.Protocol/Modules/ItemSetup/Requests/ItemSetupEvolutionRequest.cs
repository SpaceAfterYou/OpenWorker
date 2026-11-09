using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.ItemSetup.Requests;

/// <summary>
/// Evolves an upgraded item at an NPC into another item: the NPC, the slot of the item and the item as the client sees it.
/// The server reads the item from its own bag, not from the request.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct ItemSetupEvolutionRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.Evolution;

    public int Npc { get; init; }

    public StorageSlot Target { get; init; }

    public ItemValue Item { get; init; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
