using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.ItemSetup.Requests;

/// <summary>
/// Takes several items apart at an NPC.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct ItemSetupDisassembleExRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.DisassembleEx;

    public int Npc { get; init; }

    [WireArray(WireLength.Byte)]
    public IReadOnlyList<StorageSlot> Targets { get; init; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
