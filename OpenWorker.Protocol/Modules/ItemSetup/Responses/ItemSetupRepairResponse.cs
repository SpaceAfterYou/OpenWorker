using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.ItemSetup.Responses;

/// <summary>
/// An item repaired with a kit: the item, the kit used and the gold it cost.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct ItemSetupRepairResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.Repair;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public required StorageItemValue Item { get; init; }

    public int Kit { get; init; }

    public long Spent { get; init; }
}
