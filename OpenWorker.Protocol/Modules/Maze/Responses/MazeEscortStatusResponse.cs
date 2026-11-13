using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Maze.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Maze.Responses;

/// <summary>
/// The escorted creature has started its way, passed a point of it, arrived, or fallen. The client lights the point it
/// heads for and runs the function the point names.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct MazeEscortStatusResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_MAZE_ESCORT_STATUS;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    /// <summary>
    /// The escort point of the map.
    /// </summary>
    public int Point { get; init; }

    public EscortStatus Status { get; init; }
}
