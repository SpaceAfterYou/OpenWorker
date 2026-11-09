using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Types;

namespace OpenWorker.Hotspot.Modules.Items.Responses;

/// <summary>
/// The gems in the sockets of the items of the person, sent with the bags when the person comes into the game.
/// </summary>
/// <remarks>Written by hand: the list holds the options of each gem, which have no span format yet.</remarks>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct ItemSocketLoadResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.SocketLoad;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public required IReadOnlyCollection<ST_SOCKET_DATA> Sockets { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Sockets);
    }
}
