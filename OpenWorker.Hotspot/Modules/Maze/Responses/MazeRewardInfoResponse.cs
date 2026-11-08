using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Maze.Responses;

/// <summary>
/// The window shown when a maze is finished: the rank, how long it took and what the player got for it.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct MazeRewardInfoResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_MAZE_REWARD_INFO;

    public MessageOpcode Opcode => new(Group, Command);

    /// <summary>Higher is better, 0 is the lowest of six.</summary>
    public byte Rank { get; init; }
    public int PlayPoint { get; init; }
    public int ClearPoint { get; init; }
    public short Difficulty { get; init; }

    /// <summary>In seconds.</summary>
    public int ClearTime { get; init; }
    public int TotalCombo { get; init; }
    public int PlayExp { get; init; }
    public int PlayGold { get; init; }
    public int SpecialExp { get; init; }
    public int SpecialGold { get; init; }
    public int TotalExp { get; init; }
    public int TotalMoney { get; init; }
    public int TotalEther { get; init; }

    public short BaseItemCount { get; init; }
    public int BaseItem { get; init; }
    public short CashItemCount { get; init; }
    public int CashItem { get; init; }
    public short EventItemCount { get; init; }
    public byte EventItemFlag { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Rank);
        writer.Write(PlayPoint);
        writer.Write(ClearPoint);
        writer.Write(Difficulty);
        writer.Write(ClearTime);
        writer.Write(TotalCombo);
        writer.Write(PlayExp);
        writer.Write(PlayGold);
        writer.Write(SpecialExp);
        writer.Write(SpecialGold);
        writer.Write(TotalExp);
        writer.Write(TotalMoney);
        writer.Write(TotalEther);
        writer.Write(BaseItemCount);
        writer.Write(BaseItem);
        writer.Write(CashItemCount);
        writer.Write(CashItem);
        writer.Write(EventItemCount);
        writer.Write(EventItemFlag);
    }
}
