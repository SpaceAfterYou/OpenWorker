using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Maze.Requests;

/// <summary>
/// The player starts or breaks off the motion of using an object that takes time.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct MazeInteractionMotionRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_MAZE_INTERACTION_MOTION;

    /// <summary>
    /// False when the motion begins, true when the player breaks it off.
    /// </summary>
    public bool IsBroken { get; }

    public int Box { get; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
