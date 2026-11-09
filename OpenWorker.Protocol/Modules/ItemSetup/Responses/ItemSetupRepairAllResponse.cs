using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Extensions;
using OpenWorker.Hotspot.Modules.Items.Responses;

namespace OpenWorker.Hotspot.Modules.ItemSetup.Responses;

/// <summary>
/// The worn and carried items repaired at once and the gold it cost.
/// </summary>
/// <remarks>Written by hand: the item has no span format yet.</remarks>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct ItemSetupRepairAllResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.RepairAll;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public required IReadOnlyList<StorageItemValue> Items { get; init; }

    public long Spent { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Items.Count);

        foreach (var item in Items)
        {
            writer.Write(item);
        }

        writer.Write(Spent);
    }
}
