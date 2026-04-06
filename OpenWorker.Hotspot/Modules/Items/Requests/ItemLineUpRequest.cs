using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Enums;

namespace OpenWorker.Hotspot.Modules.Items.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct ItemLineUpRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.LineUp;

    public StorageGroup Storage { get; } = reader.ReadStorageGroup();
    
    public MessageOpcode Opcode => new(Group, Command);
}