using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Extensions;

namespace OpenWorker.Hotspot.Modules.Items.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct ItemOpenSlotInfoResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.OpenSlotInfo;

    public MessageOpcode Opcode => new(Group, Command);

    public required IReadOnlyList<ItemOpenSlotGroupEntry> Groups { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write((byte)Groups.Count);

        foreach (var entry in Groups)
        {
            writer.Write(entry.Group);
            writer.Write(entry.SlotCount);
            writer.Write(entry.GradeLevel);
        }
    }
}
