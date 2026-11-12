using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Items.Responses;

/// <summary>
/// What another player now wears: the actor, then the worn slots that changed, each with the item now in it (an empty slot
/// is the item <c>-1</c>). The client changes the model of that player from it.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct ItemMoveBtResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.MoveBt;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public required int Actor { get; init; }

    [WireArray(WireLength.Signed32)]
    public required IReadOnlyCollection<StorageValue> Slots { get; init; }
}
