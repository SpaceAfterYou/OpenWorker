using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Ranking.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Ranking.Responses;

/// <summary>
/// The tower in the ranking window: for each chapter how many friends cleared it, and for each stage of the chapter asked for
/// the time of the person and the fastest clear among the person and its friends. The client reads exactly fifteen chapters
/// and five stages; fewer are filled with nothing, more are left out.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct RankingInfiniteTowerListResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Ranking;
    private const RankingOpcode Command = RankingOpcode.InfiniteTowerList;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

    /// <summary>
    /// The chapters of the tower the client lists.
    /// </summary>
    public const int Chapters = 15;

    /// <summary>
    /// The stages of a chapter.
    /// </summary>
    public const int Stages = 5;

#region Message: Body

    public RankingInfiniteTowerClearInfo Data { get; init; }

#endregion Message: Body

    /// <summary>
    /// A stage nobody has cleared.
    /// </summary>
    public static RankingInfiniteTowerRankingInfo Empty { get; } = new()
    {
        MyClearTime = 0,
        Chapter = 0,
        Stage = 0,
        Person = 0,
        Hero = 0,
        Level = 0,
        Name = string.Empty,
        ClearTime = 0
    };
}
