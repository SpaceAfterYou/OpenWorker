using System.Collections.ObjectModel;
using System.Globalization;
using OpenWorker.Gameplay.Modules.Rewards;
using OpenWorker.Hotspot.Modules.DailyMissions.Enums;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.DailyMissions;

/// <summary>
/// What finishes a mission.
/// </summary>
public enum DailyMissionFinish : byte
{
    Kill = 1,
    MazeClear = 2,
    PartyMazeClear = 3,
    Friend = 4,
    Collect = 5,
    MyRoom = 6
}

/// <summary>
/// What the mission counts.
/// </summary>
public enum DailyMissionTarget : byte
{
    Monster = 1,
    MonsterGroup = 2,
    MonsterFaction = 3,
    MonsterAll = 4,
    Maze = 5,
    Person = 6,
    Item = 7
}

/// <summary>
/// The kinds of conditions a mission can have. A kill is told to the missions as a set of these.
/// </summary>
public enum DailyMissionCondition : byte
{
    None = 0,
    NormalAttack = 1,
    RiseAttack = 2,
    SkillAttack = 3,
    SubweaponAttack = 4,
    Critical = 5,
    Down = 6,
    Knockback = 7,
    DownAttack = 8,
    RankSS = 9,
    Time = 10,
    LevelGap = 11,
    Class = 12,
    Avatar = 13,
    SoulWeapon = 14,
    TitlePrefix = 15,
    TitleSuffix = 16,
    TitlePrefixSuffix = 17,
    MazeType = 18,
    MyRoomRecommend = 19,
    MyRoomPollenHelp = 20
}

/// <summary>
/// The date columns of the table: a time of day for the missions open every day, dates for the events.
/// </summary>
public readonly record struct DailyMissionDates(DateTime Start, DateTime End)
{
    private const string Format = "yyyy-MM-dd HH:mm:ss";

    /// <summary>
    /// A column that cannot be read is the year 2000, which is how an unset column looks.
    /// </summary>
    public static DailyMissionDates Parse(string start, string end) => new(ParseDate(start), ParseDate(end));

    private static DateTime ParseDate(string value)
    {
        return DateTime.TryParseExact(value, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : new DateTime(2000, 1, 1);
    }
}

public sealed record DailyMissionDefinition(
    int Id,
    DailyMissionType Type,
    int District,
    DailyMissionFinish Finish,
    DailyMissionTarget Target,
    int TargetId,
    int TargetFaction,
    int ConditionType,
    int ConditionValue,
    int ConditionClass,
    int ConditionCount,
    int MissionCount,
    DailyMissionDates Dates,
    IReadOnlyList<RewardItem> Rewards);

/// <summary>
/// What the tables say about the monsters that count for missions.
/// </summary>
public readonly record struct DailyMissionMonster(int Faction, int Group1, int Group2, int Group3);

public sealed class DailyMissionCatalog
{
    private readonly Dictionary<int, DailyMissionDefinition> _byId;
    private readonly Dictionary<int, DailyMissionMonster> _monsters;

    public DailyMissionCatalog(ReadOnlyCollection<DailyMissionRow> rows, ReadOnlyCollection<MonsterRow> monsters)
        : this(rows.Select(Create).ToArray(), monsters.ToDictionary(e => e.Id, e => new DailyMissionMonster(e.Faction, e.MissionGroup1, e.MissionGroup2, e.MissionGroup3)))
    {
    }

    private DailyMissionCatalog(DailyMissionDefinition[] definitions, Dictionary<int, DailyMissionMonster> monsters)
    {
        _byId = definitions.ToDictionary(e => e.Id);
        _monsters = monsters;
    }

    public static DailyMissionCatalog From(IEnumerable<DailyMissionDefinition> definitions, IReadOnlyDictionary<int, DailyMissionMonster>? monsters = null)
    {
        return new DailyMissionCatalog(definitions.ToArray(), monsters?.ToDictionary(e => e.Key, e => e.Value) ?? []);
    }

    public IEnumerable<DailyMissionDefinition> All => _byId.Values;

    public bool TryGet(int id, out DailyMissionDefinition definition) => _byId.TryGetValue(id, out definition!);

    public bool TryGetMonster(int id, out DailyMissionMonster monster) => _monsters.TryGetValue(id, out monster);

    internal static DailyMissionDefinition Create(DailyMissionRow row)
    {
        var rewards = new List<RewardItem>();

        AddReward(rewards, row.RewardItem1, row.RewardCount1);
        AddReward(rewards, row.RewardItem2, row.RewardCount2);
        AddReward(rewards, row.RewardItem3, row.RewardCount3);

        return new DailyMissionDefinition(
            row.Id,
            (DailyMissionType)row.Type,
            row.District,
            (DailyMissionFinish)row.Finish,
            (DailyMissionTarget)row.Target,
            row.TargetId,
            row.TargetFaction,
            row.ConditionType,
            row.ConditionValue,
            row.ConditionClass,
            row.ConditionCount,
            row.MissionCount,
            DailyMissionDates.Parse(row.OpenDate, row.CloseDate),
            rewards);
    }

    private static void AddReward(List<RewardItem> rewards, int item, byte count)
    {
        if (item > 0)
        {
            rewards.Add(new RewardItem(item, count));
        }
    }
}
