using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Items.Responses;

/// <summary>
/// The quick slots of the person: five skill cards, two items and one more number the client keeps.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct ItemLoadQuickslotResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.LoadQuickSlot;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    [WireArray(Count = 5)]
    public required int[] Cards { get; init; }

    [WireArray(Count = 2)]
    public required int[] Items { get; init; }

    /// <summary>
    /// The one more number the client keeps; the server sends zero.
    /// </summary>
    public int Extra { get; init; }
}
