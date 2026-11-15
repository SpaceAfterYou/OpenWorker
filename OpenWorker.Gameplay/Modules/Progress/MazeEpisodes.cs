using System.Collections.ObjectModel;
using OpenWorker.Hotspot.Modules.Maze.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Progress;

/// <summary>
/// A maze that is an episode of a group: its number in the story and whether it is the hard one.
/// </summary>
public readonly record struct MazeEpisode(int Maze, int Group, byte Episode, bool Hard);

/// <summary>
/// The mazes that are episodes of a group, from <c>tb_Maze_Info</c>, and the list of cleared mazes the board of the mazes is
/// opened from. Only episodes 1 to 5 count.
/// </summary>
public sealed class MazeEpisodes
{
    /// <summary>
    /// The client counts the groups in a signed byte.
    /// </summary>
    public const int MaxGroups = sbyte.MaxValue;

    private const byte FirstEpisode = 1;

    private const byte LastEpisode = 5;

    private readonly Dictionary<int, MazeEpisode> _mazes = [];

    public MazeEpisodes(ReadOnlyCollection<MazeInfoRow> rows)
        : this(rows.Select(e => new MazeEpisode(e.Id, e.Group, e.Episode, e.Difficulty != 0)))
    {
    }

    private MazeEpisodes(IEnumerable<MazeEpisode> mazes)
    {
        foreach (var maze in mazes)
        {
            if (maze.Episode is >= FirstEpisode and <= LastEpisode)
            {
                _mazes.TryAdd(maze.Maze, maze);
            }
        }
    }

    /// <summary>
    /// The episodes given.
    /// </summary>
    public static MazeEpisodes Of(IEnumerable<MazeEpisode> mazes)
    {
        return new MazeEpisodes(mazes);
    }

    public bool TryGet(int maze, out MazeEpisode episode)
    {
        return _mazes.TryGetValue(maze, out episode);
    }

    /// <summary>
    /// Every group the person has cleared an episode of, as the board reads it.
    /// </summary>
    public MazeClearValue[] Groups(IEnumerable<MazeRecord> records)
    {
        return [.. Cleared(records).GroupBy(e => e.Group).OrderBy(e => e.Key).Take(MaxGroups).Select(e => Value(e.Key, e))];
    }

    /// <summary>
    /// One group as the board reads it, with every episode of it the person has cleared.
    /// </summary>
    public MazeClearValue Group(IEnumerable<MazeRecord> records, int group)
    {
        return Value(group, Cleared(records).Where(e => e.Group == group));
    }

    private IEnumerable<MazeEpisode> Cleared(IEnumerable<MazeRecord> records)
    {
        foreach (var record in records)
        {
            if (record.Clears > 0 && _mazes.TryGetValue(record.Maze, out var episode))
            {
                yield return episode;
            }
        }
    }

    /// <summary>
    /// The board opens the episodes up to the last normal one cleared, and the hard one of each normal one cleared.
    /// </summary>
    private static MazeClearValue Value(int group, IEnumerable<MazeEpisode> cleared)
    {
        var mazes = cleared.OrderBy(e => e.Episode).ThenBy(e => e.Maze).ToArray();
        var normal = mazes.Where(e => !e.Hard).ToArray();

        return new MazeClearValue(
            group,
            normal.Length > 0 ? normal.Max(e => e.Episode) : (byte)0,
            [.. normal.Select(e => e.Maze).Take(MazeClearValue.Slots)],
            [.. mazes.Where(e => e.Hard).Select(e => e.Maze).Take(MazeClearValue.Slots)]);
    }
}
