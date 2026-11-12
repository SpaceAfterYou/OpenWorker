using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Items.Responses;

/// <summary>
/// Where an item of a sorted storage goes: the storage, the slot, its serial and its count.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial record struct ItemLineUpEntry(StorageGroup Storage, short Slot, long Serial, short Count);

/// <summary>
/// The new places of the items of a storage that has been sorted, the slot is where the item goes.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct ItemLineUpResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.LineUp;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    [WireArray(WireLength.Signed16)]
    public required ItemLineUpEntry[] Entries { get; init; }
}
