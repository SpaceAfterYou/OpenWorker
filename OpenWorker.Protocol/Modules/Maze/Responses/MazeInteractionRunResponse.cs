using OpenWorker.Hotspot.Modules.Maze.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Maze.Responses;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct MazeInteractionRunResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_MAZE_INTERACTION_RUN;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public byte Result { get; init; }
    public MazeInteractionValue Interaction { get; init; }
}
