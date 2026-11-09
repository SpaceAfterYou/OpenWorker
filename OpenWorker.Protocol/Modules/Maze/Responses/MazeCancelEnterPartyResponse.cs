using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Maze.Responses;

/// <summary>
/// The party does not enter the maze: the master called it off or a member said no. The client closes the question and
/// says who it was.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct MazeCancelEnterPartyResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_MAZE_CANCEL_ENTER_PARTY;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    /// <summary>
    /// The person who called the entry off.
    /// </summary>
    public int Actor { get; init; }
}
