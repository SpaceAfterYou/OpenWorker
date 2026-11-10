using System.Collections.ObjectModel;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// Rows of the server's tables of the mazes, written the way the files hold them.
/// </summary>
internal static class MazeRuleRows
{
    /// <summary>
    /// A row of the server's <c>tb_Maze_Info</c> with what the entry and the revive of a maze depend on.
    /// </summary>
    public static MazeRuleRow Rule(
        ushort id,
        byte type = 0,
        byte level = 0,
        ushort entryGroup = 0,
        byte limit = 0,
        uint quest = 0,
        uint item = 0,
        byte itemCount = 0,
        byte revivalType = 0,
        byte revivalItems = 0,
        uint before = 0,
        ushort portal = 0)
    {
        using var stream = new MemoryStream();
        using var w = new BinaryWriter(stream);

        w.Write(id);
        w.Write(type);
        w.Write((ushort)0);
        w.Write(level);
        w.Write((byte)0);
        w.Write((ushort)0);
        w.Write(entryGroup);
        w.Write((byte)0);
        w.Write(limit);
        w.Write(new byte[4]);
        w.Write(quest);
        w.Write(item);
        w.Write(itemCount);
        w.Write(revivalType);
        w.Write(revivalItems);
        w.Write((byte)0);
        w.Write((byte)0);
        w.Write((byte)1);
        w.Write(new byte[1 + 3 + 12]);
        w.Write(before);
        w.Write(new byte[4]);
        w.Write((ushort)0);
        w.Write(0u);
        w.Write(0u);
        w.Write((ushort)0);
        w.Write((ushort)0);
        w.Write((ushort)0);
        w.Write(0u);
        w.Write(0f);
        w.Write((ushort)0);
        w.Write(portal);
        w.Write(new byte[49]);

        stream.Position = 0;

        using var reader = new BinaryReader(stream);

        return new MazeRuleRow(reader);
    }

    public static ReadOnlyCollection<MazeRuleRow> Rules(params MazeRuleRow[] rows) => new(rows);

    /// <summary>
    /// A row of <c>tb_DailyMaze_Portal</c>: the mazes the portal opens on each day, Monday first, written as
    /// (kind, first, second) of each day; the kind 99 opens all of them.
    /// </summary>
    public static MazeDailyPortalRow Portal(ushort id, params (ushort Kind, ushort First, ushort Second)[] days)
    {
        using var stream = new MemoryStream();
        using var w = new BinaryWriter(stream);

        w.Write(id);

        foreach (var (kind, first, second) in days)
        {
            w.Write(kind);
            w.Write((ushort)0);
            w.Write(first);
            w.Write(second);
            w.Write((ushort)0);
            w.Write((ushort)0);
        }

        stream.Position = 0;

        using var reader = new BinaryReader(stream);

        return new MazeDailyPortalRow(reader);
    }

    /// <summary>
    /// A window of <c>tb_Maze_OpenControl</c>: the maze is open on the day (1 for Sunday) from the time for some minutes.
    /// </summary>
    public static MazeOpenControlRow Window(int maze, int day, int number, ushort openTime, ushort minutes)
    {
        using var stream = new MemoryStream();
        using var w = new BinaryWriter(stream);

        w.Write(MazeOpenControlRow.KeyOf(maze, day, number));
        w.Write(openTime);
        w.Write(minutes);

        stream.Position = 0;

        using var reader = new BinaryReader(stream);

        return new MazeOpenControlRow(reader);
    }
}
