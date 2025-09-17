using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Enums;

namespace OpenWorker.Hotspot.Modules.Items.Requests;

[HotspotMessage(Group, Command)]
public readonly struct ItemBreakRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.Break;

    public StorageGroup Storage { get; } = reader.ReadStorageGroup();
    public short Index { get; } = reader.ReadInt16();
    public long Serial { get; } = reader.ReadInt64();
    public int Count { get; } = reader.ReadInt32();
    
    public MessageOpcode Opcode => new(Group, Command);
}