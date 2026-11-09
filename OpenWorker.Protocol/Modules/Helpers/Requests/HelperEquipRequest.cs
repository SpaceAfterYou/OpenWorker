using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Helpers.Requests;

/// <summary>
/// Puts an item of the inventory on a helper, or takes it off.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct HelperEquipRequest : IRequestHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Helper;
    private const HelperOpcode Command = HelperOpcode.Equip;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public byte InventoryType { get; }

    public int InventoryItem { get; }

    public short InventorySlot { get; }

    public long InventorySerial { get; }

    public int Helper { get; }

    public int HelperItem { get; }

    public short HelperSlot { get; }

    public long HelperSerial { get; }

#endregion Message: Body
}
