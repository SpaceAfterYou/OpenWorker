using System.Collections.ObjectModel;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Maze;

/// <summary>
/// The mazes of the world kind (District 6): an open map that everybody of a channel shares, with no party asked and nothing
/// to clear, whose monsters come back after their time.
/// </summary>
public sealed class WorldMazes(ReadOnlyCollection<MazeInfoRow> mazes)
{
    /// <summary>
    /// The kind of maze of the world in the maze table.
    /// </summary>
    public const byte WorldKind = 5;

    /// <summary>
    /// How many players one instance takes.
    /// </summary>
    public const int Capacity = 50;

    private readonly RowIndex<short, MazeInfoRow> _mazes = mazes.IndexBy(e => e.Id);

    /// <summary>
    /// Whether the location is a maze of the world.
    /// </summary>
    public bool IsWorld(short location) => _mazes.TryGet(location, out var row) && IsWorld(row);

    public static bool IsWorld(in MazeInfoRow row) => row.Type == WorldKind;
}
