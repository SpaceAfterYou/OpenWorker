using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Items.Responses;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct ItemCombineResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.Combine;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public required ActorValue SrcActor { get; init; }
    public required StorageGroup SrcStorage { get; init; }
    public required short SrcIndex { get; init; }
    public required ItemValue SrcItem { get; init; }
    public required ActorValue DestActor { get; init; }
    public required StorageGroup DestStorage { get; init; }
    public required short DestIndex { get; init; }
    public required ItemValue DestItem { get; init; }

    /// <summary>
    /// Whether the client syncs the two items again; the server always says no.
    /// </summary>
    public bool SyncToClient { get; init; }
}
