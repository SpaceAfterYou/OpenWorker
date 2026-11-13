using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Maze.Responses;

/// <summary>
/// The unity points of the player in the unity mode of its maze: the client shows both totals on its panel.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct MazeUnityPointResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_MAZE_UPDATE_UNITY_POINT;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    /// <summary>The battle points the combos of the player have earned in the mode so far.</summary>
    public int BattlePoints { get; init; }

    /// <summary>The D.S points the player has earned in the mode so far.</summary>
    public int DsPoints { get; init; }
}
