using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Extensions;

namespace OpenWorker.Hotspot.Modules.Items.Responses;

public readonly record struct ItemLineUpEntry(StorageGroup Storage, short Slot, long Serial, short Count);

/// <summary>
/// The new places of the items of a storage that has been sorted, the slot is where the item goes.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct ItemLineUpResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.LineUp;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public required ItemLineUpEntry[] Entries { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write((short)Entries.Length);

        foreach (var entry in Entries)
        {
            writer.Write(entry.Storage);
            writer.Write(entry.Slot);
            writer.Write(entry.Serial);
            writer.Write(entry.Count);
        }
    }
}
