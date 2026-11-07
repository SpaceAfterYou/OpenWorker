using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Helpers.Requests;

/// <summary>
/// Puts an item of the inventory on a helper, or takes it off.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct HelperEquipRequest(BinaryReader reader) : IRequestHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Helper;
    private const HelperOpcode Command = HelperOpcode.Equip;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public byte InventoryType { get; } = reader.ReadByte();

    public int InventoryItem { get; } = reader.ReadInt32();

    public short InventorySlot { get; } = reader.ReadInt16();

    public long InventorySerial { get; } = reader.ReadInt64();

    public int Helper { get; } = reader.ReadInt32();

    public int HelperItem { get; } = reader.ReadInt32();

    public short HelperSlot { get; } = reader.ReadInt16();

    public long HelperSerial { get; } = reader.ReadInt64();

#endregion Message: Body
}
