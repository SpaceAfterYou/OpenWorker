using System.Collections.ObjectModel;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Ranking;

/// <summary>
/// The reward of a place of a time attack ranking at the end of a period.
/// </summary>
public readonly record struct PlaceReward(int Place, int Item, int Count);

/// <summary>
/// The maps of the time attack ranking and the rewards of their places, from the client's reward table: the client lists
/// these maps in the raid tab of its ranking window and shows these rewards in its popup.
/// </summary>
public sealed class TimeAttackRewards
{
    private readonly Dictionary<int, PlaceReward[]> _maps;

    public TimeAttackRewards(ReadOnlyCollection<RankRewardRow> rows)
        : this(rows.Select(e => (e.Map, new PlaceReward(e.Place, e.Item, e.Count))))
    {
    }

    private TimeAttackRewards(IEnumerable<(short Map, PlaceReward Reward)> rows)
    {
        _maps = rows
            .Where(e => e.Map > 0 && e.Reward.Place > 0)
            .GroupBy(e => (int)e.Map)
            .ToDictionary(e => e.Key, e => e.Select(x => x.Reward).DistinctBy(x => x.Place).OrderBy(x => x.Place).ToArray());
    }

    public static TimeAttackRewards From(params (short Map, short Place, int Item, int Count)[] rows)
    {
        return new TimeAttackRewards(rows.Select(e => (e.Map, new PlaceReward(e.Place, e.Item, e.Count))));
    }

    public IEnumerable<int> Maps => _maps.Keys;

    /// <summary>
    /// Whether the client keeps a time attack list of the map.
    /// </summary>
    public bool IsRanked(int map) => _maps.ContainsKey(map);

    /// <summary>
    /// The rewarded places of the map, in order.
    /// </summary>
    public IReadOnlyList<PlaceReward> Of(int map) => _maps.TryGetValue(map, out var places) ? places : [];
}
