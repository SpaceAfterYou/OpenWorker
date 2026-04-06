using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Maze.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct MazeOperationEndRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_OPERATION_END_REQ;

    public int Operation { get; } = reader.ReadInt32();

    public MessageOpcode Opcode => new(Group, Command);
}