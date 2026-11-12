using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.ItemSetup.Responses;

/// <summary>
/// The worn items repaired at once and the gold it cost.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct ItemSetupRepairEquipResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.RepairEquip;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    [WireArray(WireLength.Signed32)]
    public required IReadOnlyList<StorageItemValue> Items { get; init; }

    public long Spent { get; init; }
}
