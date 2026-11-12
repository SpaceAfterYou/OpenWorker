using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.MyRoom.Requests;

/// <summary>
/// The person leaves the room it stands in, back to where it came from.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct MyRoomExitRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.MyRoom;
    private const MyRoomOpcode Command = MyRoomOpcode.ExitReq;

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
