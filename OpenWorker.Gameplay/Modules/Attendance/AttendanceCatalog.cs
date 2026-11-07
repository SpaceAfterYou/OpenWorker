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
            tables.Select(e => new AttendanceTable(e.Id, e.Field2)),
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
        int[] items = [row.Field4, row.Field5, row.Field6, row.Field7, row.Field8];
        int[] counts = [row.Field9, row.Field10, row.Field11, row.Field12, row.Field13];

        return new Reward(
            items.Zip(counts)
                .Where(e => e is { First: > 0, Second: > 0 })
                .Select(e => new RewardItem(e.First, (short)e.Second))
                .ToArray(),
            row.Field2,
            row.Field3);
    }

    private static PlayTimeStep[] Steps(CheckAccessRewardRow row)
    {
        return
        [
            new PlayTimeStep(row.Field1, row.Field4),
            new PlayTimeStep(row.Field2, row.Field5),
            new PlayTimeStep(row.Field3, row.Field6)
        ];
    }

    /// <summary>
    /// A step that waits for no time is not there.
    /// </summary>
    public bool HasStep(int step) => step >= 0 && step < PlayTime.Count && PlayTime[step].Seconds > 0;
}
