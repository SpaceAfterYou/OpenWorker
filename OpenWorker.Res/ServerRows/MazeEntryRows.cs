using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

// The tables the server reads to decide when a maze may be entered. The client has neither.

/// <summary>
/// A window in which a maze is open: on one day of the week, from a time of the day for some minutes. The key is the maze, the
/// day (1 for Sunday to 7 for Saturday) and the number of the window, one after the other from 1. A maze with no window on
/// the day is open the whole day.
/// </summary>
[BinaryResourceTable("tb_Maze_OpenControl", Optional = true)]
public readonly struct MazeOpenControlRow(BinaryReader reader)
{
    /// <summary>The most windows a maze has on one day.</summary>
    public const int MaxWindows = 20;

    /// <summary>The maze times 10000, plus the day of the week times 100, plus the number of the window.</summary>
    public uint Id { get; } = reader.ReadUInt32();

    /// <summary>The time of the day the window opens at, as hours and minutes written together (1930 is half past seven in the evening).</summary>
    public ushort OpenTime { get; } = reader.ReadUInt16();

    /// <summary>The minutes the window stays open.</summary>
    public ushort RemainMinutes { get; } = reader.ReadUInt16();

    /// <summary>The key of the window of a maze on a day of the week (1 for Sunday), the number of the window from 1.</summary>
    public static uint KeyOf(int maze, int day, int number) => (uint)maze * 10_000u + (uint)day * 100u + (uint)number;
}

/// <summary>
/// The casual raids of a portal take turns: on each day of the week the portal opens some of its mazes. A day of kind 99
/// opens all the mazes of the portal; the first maze of a day is the one whose count the others share.
/// </summary>
[BinaryResourceTable("tb_DailyMaze_Portal", Optional = true)]
public readonly struct MazeDailyPortalRow
{
    /// <summary>The kind of a day that opens every maze of the portal.</summary>
    public const ushort AllMazes = 99;

    private const int Days = 7;
    private const int Episodes = 4;

    private readonly ushort[] _kinds;
    private readonly ushort[][] _mazes;

    public MazeDailyPortalRow(BinaryReader reader)
    {
        Id = reader.ReadUInt16();
        _kinds = new ushort[Days];
        _mazes = new ushort[Days][];

        // The file keeps Monday first and Sunday last.
        for (var day = 0; day < Days; day++)
        {
            _kinds[day] = reader.ReadUInt16();

            // The text the client shows for the day.
            reader.ReadUInt16();

            _mazes[day] = new ushort[Episodes];

            for (var episode = 0; episode < Episodes; episode++)
            {
                _mazes[day][episode] = reader.ReadUInt16();
            }
        }
    }

    public ushort Id { get; }

    /// <summary>
    /// Whether the portal opens the maze on the day, and which maze is the first of that day.
    /// </summary>
    public bool Opens(DayOfWeek day, int maze, out int first)
    {
        first = 0;

        if (_kinds is null)
        {
            return false;
        }

        // Sunday is the last day of the file, Monday the first.
        var index = ((int)day + 6) % Days;

        first = _mazes[index][0];

        return _kinds[index] == AllMazes || (maze is > 0 and <= ushort.MaxValue && Array.IndexOf(_mazes[index], (ushort)maze) >= 0);
    }
}
