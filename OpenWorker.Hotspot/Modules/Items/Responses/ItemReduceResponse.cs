using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Extensions;

namespace OpenWorker.Hotspot.Modules.Items.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct ItemReduceResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.Reduce;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public StorageGroup Storage { get; init; }
    public short Index { get; init; }
    public int Count { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Storage);
        writer.Write(Index);
        writer.Write(Count);
    }

#endregion Interface: IWritableData
}
