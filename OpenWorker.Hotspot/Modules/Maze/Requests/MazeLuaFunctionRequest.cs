using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Maze.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct MazeLuaFunctionRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_MAZE_LUA_FUNCTION_REQ;

    public int Box { get; } = reader.ReadInt32();

    public MessageOpcode Opcode => new(Group, Command);
}