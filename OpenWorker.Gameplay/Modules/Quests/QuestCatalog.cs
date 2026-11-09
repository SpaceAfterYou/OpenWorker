using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using OpenWorker.Hotspot.Modules.Quests.Enums;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Quests;

/// <summary>
/// The episodes and conditions of the tables, in the form the rules use, and what the monsters count for.
/// </summary>
public sealed class QuestCatalog
{
    private readonly Dictionary<int, QuestDefinition> _episodes;
    private readonly Dictionary<int, QuestConditionDefinition> _conditions;
    private readonly Dictionary<int, int[]> _monsterGroups;
    private readonly Dictionary<int, int[]> _classRewards;
    private readonly Dictionary<int, int[]> _classTitles = [];

    public QuestCatalog(
        ReadOnlyCollection<QuestEpisodeRow> episodes,
        ReadOnlyCollection<QuestConditionRow> conditions,
        ReadOnlyCollection<QuestRewardRow> rewards,
        ReadOnlyCollection<MonsterRow> monsters,
        ReadOnlyCollection<TitleRewardRow>? titles = null)
    {
        _conditions = conditions.Select(e => e.ToDefinition()).ToDictionary(e => e.Id);

        // An episode counts its conditions until the first one the table does not have.
        _episodes = episodes
            .Select(e => e.ToDefinition())
            .Select(e => e with { Conditions = e.Conditions.TakeWhile(_conditions.ContainsKey).ToArray() })
            .ToDictionary(e => e.Id);

        _classRewards = rewards.ToDictionary(e => (int)e.Id, e => e.Items());
        _monsterGroups = monsters
            .Select(e => (e.Id, Groups: e.QuestGroups()))
            .Where(e => e.Groups.Length > 0)
            .ToDictionary(e => e.Id, e => e.Groups);

        if (titles is not null)
        {
            _classTitles = titles
                .GroupBy(e => e.Id)
                .ToDictionary(e => e.Key, e => e.First().Titles());
        }
    }

    private QuestCatalog(
        IEnumerable<QuestDefinition> episodes,
        IEnumerable<QuestConditionDefinition> conditions,
        IReadOnlyDictionary<int, int[]> classRewards,
        IReadOnlyDictionary<int, int[]> monsterGroups)
    {
        _episodes = episodes.ToDictionary(e => e.Id);
        _conditions = conditions.ToDictionary(e => e.Id);
        _classRewards = new Dictionary<int, int[]>(classRewards);
        _monsterGroups = new Dictionary<int, int[]>(monsterGroups);
    }

    /// <summary>
    /// A catalog of its own, for tests and for tools.
    /// </summary>
    public static QuestCatalog From(
        IEnumerable<QuestDefinition> episodes,
        IEnumerable<QuestConditionDefinition> conditions,
        IReadOnlyDictionary<int, int[]>? classRewards = null,
        IReadOnlyDictionary<int, int[]>? monsterGroups = null)
    {
        return new QuestCatalog(episodes, conditions, classRewards ?? new Dictionary<int, int[]>(), monsterGroups ?? new Dictionary<int, int[]>());
    }

    public IEnumerable<QuestDefinition> Episodes => _episodes.Values;

    public IEnumerable<QuestConditionDefinition> Conditions => _conditions.Values;

    /// <summary>
    /// The episodes that are taken by themselves once the episode before them is finished.
    /// </summary>
    public IReadOnlyList<QuestDefinition> StartingByThemselves => _startingByThemselves ??= _episodes.Values
        .Where(e => e.StartsByItself && e.Before.Count > 0)
        .OrderBy(e => e.Id)
        .ToArray();

    private QuestDefinition[]? _startingByThemselves;

    public bool TryGetEpisode(int id, [NotNullWhen(true)] out QuestDefinition? episode) => _episodes.TryGetValue(id, out episode);

    public bool TryGetCondition(int id, [NotNullWhen(true)] out QuestConditionDefinition? condition) => _conditions.TryGetValue(id, out condition);

    /// <summary>
    /// The groups a monster counts for, on top of the monster itself.
    /// </summary>
    public IReadOnlyList<int> GetMonsterGroups(int monster) => _monsterGroups.TryGetValue(monster, out var groups) ? groups : [];

    /// <summary>
    /// The title an episode that names a group of titles gives to a hero: the group's title for the class, or the number
    /// itself when the tables have no such group.
    /// </summary>
    public int GetClassTitle(int group, int hero)
    {
        return group != 0 && _classTitles.TryGetValue(group, out var titles) && hero >= 1 && hero <= titles.Length && titles[hero - 1] != 0
            ? titles[hero - 1]
            : group;
    }

    /// <summary>
    /// The item a row of the table of rewards gives to a hero, or 0.
    /// </summary>
    public int GetClassReward(int row, int hero)
    {
        return _classRewards.TryGetValue(row, out var items) && hero >= 1 && hero <= items.Length ? items[hero - 1] : 0;
    }
}
