using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Items.Responses;

/// <summary>
/// Gems that changed in the sockets of items: the client replaces each socket it is told about.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct ItemSocketUpdateResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.SocketUpdate;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    [WireArray(WireLength.Byte)]
    public required IReadOnlyCollection<ST_SOCKET_DATA> Sockets { get; init; }
}
