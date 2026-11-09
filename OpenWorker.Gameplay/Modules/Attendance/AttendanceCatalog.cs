using System.Collections.ObjectModel;
using OpenWorker.Gameplay.Modules.Rewards;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Attendance;

/// <summary>
/// A table of days of attendance. Its rewards are the rows numbered 100 times the table plus the day.
/// </summary>
public readonly record struct AttendanceTable(int Id, int Days);

/// <summary>
/// The reward for staying in the game: after every wait the person gets an item.
/// </summary>
public readonly record struct PlayTimeStep(int Seconds, int Item);

public sealed class AttendanceCatalog
{
    private readonly Dictionary<int, AttendanceTable> _tables;
    private readonly Dictionary<int, Reward> _rewards;

    public AttendanceCatalog(
        ReadOnlyCollection<CheckAttendanceInfoRow> tables,
        ReadOnlyCollection<CheckAttendanceRewardRow> rewards,
        ReadOnlyCollection<CheckAccessRewardRow> playTime)
        : this(
            tables.Select(e => new AttendanceTable(e.Id, e.Days)),
            rewards.ToDictionary(e => e.Id, Create),
            playTime.Where(e => e.Id == 1).Select(Steps).FirstOrDefault() ?? [])
    {
    }

    private AttendanceCatalog(IEnumerable<AttendanceTable> tables, Dictionary<int, Reward> rewards, PlayTimeStep[] steps)
    {
        _tables = tables.ToDictionary(e => e.Id);
        _rewards = rewards;
        PlayTime = steps;
    }

    public static AttendanceCatalog From(IEnumerable<AttendanceTable> tables, IReadOnlyDictionary<int, Reward> rewards, PlayTimeStep[] steps)
    {
        return new AttendanceCatalog(tables, rewards.ToDictionary(e => e.Key, e => e.Value), steps);
    }

    /// <summary>
    /// What the person gets for staying in the game, in the order of the steps.
    /// </summary>
    public IReadOnlyList<PlayTimeStep> PlayTime { get; }

    /// <summary>
    /// The table of the month, or the only one when the tables do not go by month.
    /// </summary>
    public bool TryGetTable(DateTimeOffset now, out AttendanceTable table)
    {
        if (_tables.TryGetValue(1000 + now.Month, out table))
        {
            return true;
        }

        table = _tables.Values.OrderBy(e => e.Id).FirstOrDefault();

        return table.Id != 0;
    }

    public bool TryGetTable(int id, out AttendanceTable table) => _tables.TryGetValue(id, out table);

    public Reward GetReward(int table, int day) =>
        _rewards.TryGetValue(table * 100 + day, out var reward) ? reward : Reward.Empty;

    private static Reward Create(CheckAttendanceRewardRow row)
    {
        int[] items = [row.Item1, row.Item2, row.Item3, row.Item4, row.Item5];
        int[] counts = [row.Count1, row.Count2, row.Count3, row.Count4, row.Count5];

        return new Reward(
            items.Zip(counts)
                .Where(e => e is { First: > 0, Second: > 0 })
                .Select(e => new RewardItem(e.First, (short)e.Second))
                .ToArray(),
            row.Gold,
            row.Ether);
    }

    private static PlayTimeStep[] Steps(CheckAccessRewardRow row)
    {
        return
        [
            new PlayTimeStep(row.Seconds1, row.Item1),
            new PlayTimeStep(row.Seconds2, row.Item2),
            new PlayTimeStep(row.Seconds3, row.Item3)
        ];
    }

    /// <summary>
    /// A step that waits for no time is not there.
    /// </summary>
    public bool HasStep(int step) => step >= 0 && step < PlayTime.Count && PlayTime[step].Seconds > 0;
}
