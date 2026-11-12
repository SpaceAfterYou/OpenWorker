using OpenWorker.Domain.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Ranking.Types;

/// <summary>
/// One stage of the chapter asked for: the time of the person who asks, and the fastest clear of the stage among the
/// person and its friends.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial record struct RankingInfiniteTowerRankingInfo
{
    /// <summary>
    /// The fastest clear of the stage by the person who asks, in seconds; 0 for none.
    /// </summary>
    public required int MyClearTime { get; init; }

    /// <summary>
    /// The chapter of the fastest clear; the client files the clear under it.
    /// </summary>
    public required int Chapter { get; init; }

    /// <summary>
    /// The stage of the fastest clear, 1 to 5; the client leaves a stage of 0 out.
    /// </summary>
    public required int Stage { get; init; }

    public required int Person { get; init; }
    [WireAs(typeof(byte))]
    public required Hero Hero { get; init; }
    public required byte Level { get; init; }
    [WireString(WireStringFormat.Utf16Unicode)]
    public required string Name { get; init; }

    /// <summary>
    /// The fastest clear, in seconds.
    /// </summary>
    public required int ClearTime { get; init; }
}
