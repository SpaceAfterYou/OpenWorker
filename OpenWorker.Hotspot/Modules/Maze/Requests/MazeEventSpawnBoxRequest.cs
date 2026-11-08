using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Maze.Requests;

[WireMessage]
[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly partial struct MazeEventSpawnBoxRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_EVENT_SPAWN_BOX_REQ;

    public int Box { get; }

    public MessageOpcode Opcode => new(Group, Command);
}