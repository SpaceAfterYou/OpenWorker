using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.ItemSetup.Requests;

/// <summary>
/// Puts a broach on a piece of clothing: the clothing first, then the broach and the place it goes to (0 to 14).
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct ItemSetupBroachEquipRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.BroachEquip;

    public StorageSlot Target { get; init; }

    public StorageSlot Broach { get; init; }

    public byte Place { get; init; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
