using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Hotspot.Modules.DailyMissions.Enums;

namespace OpenWorker.Gameplay.Modules.DailyMissions;

/// <summary>
/// When missions are open, which ones a person gets and what counts as progress. Times are those of the
/// server, which is what the tables are written for.
/// </summary>
public static class DailyMissionRules
{
    /// <summary>
    /// How many missions can be shown in the tracker at once.
    /// </summary>
    public const int MaxHelpers = 4;

    /// <summary>
    /// The day of the special missions starts at this time.
    /// </summary>
    public static readonly TimeSpan DefaultResetTime = TimeSpan.FromHours(11);

    /// <summary>
    /// The day that is running at <paramref name="now"/>: it started at the last reset and lasts until the next.
    /// </summary>
    public static (DateTimeOffset Start, DateTimeOffset End) GetDay(DateTimeOffset now, TimeSpan reset) =>
        GameDay.Get(now, reset);

    /// <summary>
    /// The time a mission is open, as the person sees it today.
    /// </summary>
    public static (DateTimeOffset Start, DateTimeOffset End) GetWindow(DailyMissionDefinition definition, DateTimeOffset now, TimeSpan reset)
    {
        switch (definition.Type)
        {
            case DailyMissionType.Special:
                return GetDay(now, reset);

            // Only the time of day matters, the date of the columns is a placeholder.
            case DailyMissionType.Guerrilla:
                return (
                    new DateTimeOffset(now.Date + definition.Dates.Start.TimeOfDay, now.Offset),
                    new DateTimeOffset(now.Date + definition.Dates.End.TimeOfDay, now.Offset));

            default:
                return (
                    new DateTimeOffset(definition.Dates.Start, now.Offset),
                    new DateTimeOffset(definition.Dates.End, now.Offset));
        }
    }

    /// <summary>
    /// Whether the time of the mission has come and not yet gone. A special mission goes by the day it was drawn
    /// for, the others by the table.
    /// </summary>
    public static bool IsOpen(DailyMissionDefinition definition, DailyMissionEntry entry, DateTimeOffset now)
    {
        switch (definition.Type)
        {
            case DailyMissionType.Special:
                return entry.Start <= now && now < entry.End;

            case DailyMissionType.Guerrilla:
                var time = now.TimeOfDay;

                return definition.Dates.Start.TimeOfDay <= time && time < definition.Dates.End.TimeOfDay;

            case DailyMissionType.Event:
                var (start, end) = GetWindow(definition, now, TimeSpan.Zero);

                return start <= now && now < end;

            default:
                return false;
        }
    }

    /// <summary>
    /// Seconds before the mission opens.
    /// </summary>
    public static int GetRemain(DateTimeOffset now, DateTimeOffset start) =>
        now < start ? (int)(start - now).TotalSeconds : 0;

    /// <summary>
    /// Seconds the mission stays open from now on, or from its start if that is still to come.
    /// </summary>
    public static int GetDuration(DateTimeOffset now, DateTimeOffset start, DateTimeOffset end)
    {
        if (end <= now)
        {
            return 0;
        }

        return (int)((now < start ? end - start : end - now).TotalSeconds);
    }

    /// <summary>
    /// Whether one more mission of the finish type can be drawn: 0 yes, 1 the type is full, 2 this finish type is full.
    /// </summary>
    public static int CheckCount(DailyMissionType type, int typeCount, DailyMissionFinish finish, int finishCount)
    {
        switch (type)
        {
            case DailyMissionType.Special:
                if (typeCount >= 5)
                {
                    return 1;
                }

                return finish switch
                {
                    DailyMissionFinish.Kill when finishCount >= 2 => 2,
                    DailyMissionFinish.MazeClear or DailyMissionFinish.PartyMazeClear or DailyMissionFinish.Friend when finishCount >= 1 => 2,
                    _ => 0
                };

            case DailyMissionType.Guerrilla:
                if (typeCount >= 1)
                {
                    return 1;
                }

                return finish is DailyMissionFinish.Kill or DailyMissionFinish.MazeClear && finishCount >= 1 ? 2 : 0;

            default:
                return 0;
        }
    }

    /// <summary>
    /// Draws the missions of a type for a person: for every district a few of the table, in random order,
    /// within the limits of the type.
    /// </summary>
    public static List<int> Draw(DailyMissionCatalog catalog, DailyMissionType type, DateTimeOffset now, Random random)
    {
        var result = new List<int>();

        foreach (var district in catalog.All.Where(e => e.Type == type).GroupBy(e => e.District).OrderBy(e => e.Key))
        {
            var candidates = district.OrderBy(_ => random.Next()).ToArray();
            var typeCount = 0;
            var finishCount = new int[8];

            for (var i = 0; i < candidates.Length; i++)
            {
                var candidate = candidates[i];
                var check = CheckCount(type, typeCount, candidate.Finish, finishCount[(int)candidate.Finish]);

                if (check == 1)
                {
                    break;
                }

                if (check == 2)
                {
                    continue;
                }

                // A mission of the day that has already begun is left for a later one, unless it is the last.
                if (type == DailyMissionType.Guerrilla &&
                    candidate.Dates.Start.TimeOfDay < now.TimeOfDay &&
                    i + 1 < candidates.Length)
                {
                    continue;
                }

                if (type == DailyMissionType.Event && GetWindow(candidate, now, TimeSpan.Zero).End <= now)
                {
                    continue;
                }

                result.Add(candidate.Id);
                typeCount++;
                finishCount[(int)candidate.Finish]++;
            }
        }

        return result;
    }

    /// <summary>
    /// Whether a kill counts for the mission. The kinds of attack that killed are given as a set of conditions.
    /// </summary>
    public static bool CountsKill(DailyMissionDefinition definition, DailyMissionCatalog catalog, int monster, uint conditions)
    {
        var condition = definition.ConditionType is >= 0 and < 32 && (conditions & (1u << definition.ConditionType)) != 0;

        switch (definition.Target)
        {
            // An event mission does not ask for the kind of attack.
            case DailyMissionTarget.Monster:
                return (definition.Type == DailyMissionType.Event || condition) && definition.TargetId == monster;

            case DailyMissionTarget.MonsterGroup:
                return condition &&
                       catalog.TryGetMonster(monster, out var grouped) &&
                       (definition.TargetId == grouped.Group1 || definition.TargetId == grouped.Group2 || definition.TargetId == grouped.Group3);

            case DailyMissionTarget.MonsterFaction:
                return condition && catalog.TryGetMonster(monster, out var faction) && definition.TargetFaction == faction.Faction;

            case DailyMissionTarget.MonsterAll:
                return condition;

            default:
                return false;
        }
    }

    /// <summary>
    /// Whether a cleared maze counts for the mission.
    /// </summary>
    public static bool CountsMazeClear(DailyMissionDefinition definition, DailyMissionMazeClear clear)
    {
        if (definition.TargetId != 0 && definition.TargetId != clear.Maze)
        {
            return false;
        }

        switch ((DailyMissionCondition)definition.ConditionType)
        {
            case DailyMissionCondition.None:
                return true;

            case DailyMissionCondition.RankSS:
                return clear.Rank >= 5;

            case DailyMissionCondition.Time:
                return definition.ConditionValue >= clear.Seconds;

            // Enough of the others in the party have about the same level.
            case DailyMissionCondition.LevelGap:
                return definition.ConditionCount <= clear.Party.Count(e => Math.Abs(e.Level - clear.Level) <= definition.ConditionValue);

            // Enough of the others in the party are of the class, 7 meaning the class of the person.
            case DailyMissionCondition.Class:
                var wanted = definition.ConditionClass == 7 ? clear.Class : definition.ConditionClass;

                return definition.ConditionCount <= clear.Party.Count(e => e.Class == wanted);

            case DailyMissionCondition.MazeType:
                return definition.ConditionValue == clear.MazeType;

            default:
                return false;
        }
    }
}

/// <summary>
/// A member of the party that cleared a maze together with the person.
/// </summary>
public readonly record struct DailyMissionPartyMember(int Level, int Class);

/// <summary>
/// A maze the person has cleared.
/// </summary>
public sealed record DailyMissionMazeClear(
    int Maze,
    int Rank,
    int Seconds,
    int MazeType,
    int Level,
    int Class,
    IReadOnlyList<DailyMissionPartyMember> Party)
{
    public DailyMissionMazeClear(int maze, int rank, int seconds) : this(maze, rank, seconds, 0, 0, 0, [])
    {
    }
}
