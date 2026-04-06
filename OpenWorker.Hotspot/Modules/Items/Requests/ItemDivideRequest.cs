using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Enums;

namespace OpenWorker.Hotspot.Modules.Items.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct ItemDivideRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.Divide;

    public MoveItemValue Src { get; } = new(reader);
    
    public StorageGroup DestStorage { get; } = reader.ReadStorageGroup();
    public short DestIndex { get; } = reader.ReadInt16();
    public short Count { get; } = reader.ReadInt16();
    
    public MessageOpcode Opcode => new(Group, Command);
}