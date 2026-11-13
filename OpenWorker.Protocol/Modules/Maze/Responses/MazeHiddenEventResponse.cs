using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Maze.Responses;

/// <summary>
/// The hidden event of the maze: its condition shown when it starts, its count as it goes, then whether it was met. The client
/// reads the condition from its own table and skips the event.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct MazeHiddenEventResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_HIDDEN_EVENT_INFO;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    /// <summary>The hidden event: the id of the box that started it.</summary>
    public ushort Event { get; init; }

    /// <summary>The row of the condition the event asks for.</summary>
    public int Condition { get; init; }

    /// <summary>1 started, 2 counted, 3 met, 4 failed.</summary>
    public byte State { get; init; }

    /// <summary>What the condition has counted so far.</summary>
    public int Count { get; init; }
}
