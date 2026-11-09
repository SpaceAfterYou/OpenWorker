using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.ItemSetup.Requests;

/// <summary>
/// Repairs one item at an NPC.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct ItemSetupRepairNpcRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.RepairNpc;

    public int Npc { get; init; }

    public StorageSlot Target { get; init; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
