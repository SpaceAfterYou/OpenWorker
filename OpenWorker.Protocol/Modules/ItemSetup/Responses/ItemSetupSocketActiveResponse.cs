using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Extensions;
using OpenWorker.Hotspot.Modules.Items.Responses;

namespace OpenWorker.Hotspot.Modules.ItemSetup.Responses;

/// <summary>
/// The item with its new socket open, and the socket.
/// </summary>
/// <remarks>Written by hand: the item has no span format yet.</remarks>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct ItemSetupSocketActiveResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.SocketActive;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public required StorageItemValue Item { get; init; }

    public byte Socket { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Item);
        writer.Write(Socket);
    }
}
