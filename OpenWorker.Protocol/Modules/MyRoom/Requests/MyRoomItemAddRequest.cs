using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.MyRoom.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.MyRoom.Requests;

/// <summary>
/// The owner places a piece of furniture from the furniture bag.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct MyRoomItemAddRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.MyRoom;
    private const MyRoomOpcode Command = MyRoomOpcode.ItemAdd;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public MyRoomFurnitureValue Piece { get; }
}
