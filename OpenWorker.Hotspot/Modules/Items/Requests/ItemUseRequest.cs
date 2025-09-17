using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Types;

namespace OpenWorker.Hotspot.Modules.Items.Requests;

[HotspotMessage(Group, Command)]
public readonly struct ItemUseRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.Use;

    public StorageGroup Storage { get; } = reader.ReadStorageGroup();
    public short Slot { get; } = reader.ReadInt16();
    public SerialValue Serial { get; } = new(reader);
    
    public MessageOpcode Opcode => new(Group, Command);
}