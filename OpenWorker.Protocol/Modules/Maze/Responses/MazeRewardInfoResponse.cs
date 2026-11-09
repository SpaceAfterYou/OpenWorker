using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Maze.Responses;

/// <summary>
/// The window shown when a maze is finished: the rank, the score, how long it took and what the player got for it. The
/// order of the fields is the client's: it splits the time into hours, minutes and seconds, shows the item and the count of
/// each reward when both are above zero, and writes the gold of the run to the chat.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct MazeRewardInfoResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_MAZE_REWARD_INFO;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    /// <summary>Higher is better: 0 is F, then C, B, A, S, and 5 is SS.</summary>
    public byte Rank { get; init; }

    /// <summary>The score of the run.</summary>
    public int PlayPoint { get; init; }

    /// <summary>The points the clear itself is worth, the first part of the score.</summary>
    public int ClearPoint { get; init; }

    /// <summary>In seconds.</summary>
    public ushort ClearTime { get; init; }

    /// <summary>The longest combo of the run.</summary>
    public int TotalCombo { get; init; }

    /// <summary>The experience of the clear.</summary>
    public int PlayExp { get; init; }

    /// <summary>The gold of the clear.</summary>
    public int PlayGold { get; init; }

    /// <summary>What a booster adds to the experience of the clear.</summary>
    public int SpecialExp { get; init; }

    /// <summary>What a booster adds to the gold of the clear.</summary>
    public int SpecialGold { get; init; }

    /// <summary>All the experience of the run, the clear included.</summary>
    public int TotalExp { get; init; }

    /// <summary>All the gold of the run, the clear included.</summary>
    public int TotalMoney { get; init; }

    public int TotalEther { get; init; }

    /// <summary>The item of the rank, given with the window.</summary>
    public int BaseItem { get; init; }

    public short BaseItemCount { get; init; }

    /// <summary>The item the keys open (08/47).</summary>
    public int CashItem { get; init; }

    public short CashItemCount { get; init; }

    /// <summary>The item of a hidden event of the maze.</summary>
    public int EventItem { get; init; }

    public short EventItemCount { get; init; }

    /// <summary>Whether the item of the hidden event went by post.</summary>
    public byte EventItemByPost { get; init; }
}
