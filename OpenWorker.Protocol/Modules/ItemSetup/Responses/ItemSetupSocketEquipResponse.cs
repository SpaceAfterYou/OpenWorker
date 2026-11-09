using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Types;

namespace OpenWorker.Hotspot.Modules.ItemSetup.Responses;

/// <summary>
/// The gems that went into the sockets of an item, each with the serial of the item, its gem, its place and its options.
/// </summary>
/// <remarks>Written by hand: the options of a gem have no span format yet.</remarks>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct ItemSetupSocketEquipResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.SocketEquip;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public required IReadOnlyCollection<ST_SOCKET_DATA> Sockets { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Sockets);
    }
}
