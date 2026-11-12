using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.InfiniteTower.Responses;

/// <summary>
/// How far the person has climbed the tower and how often it went up today. The client opens the chapters up to the one after
/// <see cref="ClearChapter"/> in its window of the tower and keeps the rest locked.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct InfiniteTowerLoadInfoResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.InfiniteTower;
    private const InfiniteTowerOpcode Command = InfiniteTowerOpcode.LoadInfo;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    /// <summary>
    /// The last chapter whose five stages the person has cleared, 0 for none.
    /// </summary>
    public short ClearChapter { get; init; }

    /// <summary>
    /// The highest stage the person has cleared, counted over the whole tower (stage + 5 × (chapter − 1)).
    /// </summary>
    public short ClearStage { get; init; }

    /// <summary>
    /// Seconds until the count of the day is cleared, 0 once it is due.
    /// </summary>
    public long LeftTime { get; init; }

    /// <summary>
    /// How many chapters the person has entered today.
    /// </summary>
    public short Count { get; init; }
}
