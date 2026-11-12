using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.MyRoom.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.MyRoom.Responses;

/// <summary>
/// The room the player has come into.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct MyRoomEnterResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.MyRoom;
    private const MyRoomOpcode Command = MyRoomOpcode.EnterReq;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public required MyRoomOwnerValue Room { get; init; }
}
