using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Maze.Requests;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct MazeCompleteRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_COMPLETE_MAZE_REQ;

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
