using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.InfiniteTower.Responses;

/// <summary>
/// A stage of the tower is cleared: what the person got for it. The client shows its window of the clear with the time and
/// the extra item; the reward of the stage itself it reads from its own table.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct InfiniteTowerRewardResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.InfiniteTower;
    private const InfiniteTowerOpcode Command = InfiniteTowerOpcode.Reward;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    /// <summary>
    /// The item of the stage.
    /// </summary>
    public int Item { get; init; }

    public short Count { get; init; }

    /// <summary>
    /// The battle points of the stage.
    /// </summary>
    public int BattlePoint { get; init; }

    /// <summary>
    /// The item of a hidden event of the stage, 0 for none.
    /// </summary>
    public int ExtraItem { get; init; }

    public short ExtraCount { get; init; }

    /// <summary>
    /// How long the stage took, in seconds.
    /// </summary>
    public int ClearTime { get; init; }
}
