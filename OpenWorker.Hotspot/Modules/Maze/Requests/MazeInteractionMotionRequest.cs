using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Maze.Requests;

/// <summary>
/// The player starts or breaks off the motion of using an object that takes time.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct MazeInteractionMotionRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_MAZE_INTERACTION_MOTION;

    /// <summary>
    /// False when the motion begins, true when the player breaks it off.
    /// </summary>
    public bool IsBroken { get; } = reader.ReadBoolean();

    public int Box { get; } = reader.ReadInt32();

    public MessageOpcode Opcode => new(Group, Command);
}
