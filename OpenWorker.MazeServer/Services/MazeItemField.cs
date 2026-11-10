using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Lua;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// The maze as the place items act in: what its table says about items (a few mazes forbid them, the infinite tower takes
/// none), whether the person is on its feet, and the buff a potion gives, which the maze carries and shows.
/// </summary>
public sealed class MazeItemField(
    World world,
    ReadOnlyCollection<MazeInfoRow> mazes,
    ReadOnlyCollection<MazeRuleRow> rules) : IItemField
{
    /// <summary>
    /// The kind of maze that is the infinite tower.
    /// </summary>
    private const int InfiniteTowerType = 7;

    private readonly RowIndex<short, MazeInfoRow> _mazes = mazes.IndexBy(e => e.Id);
    private readonly RowIndex<ushort, MazeRuleRow> _rules = rules.IndexBy(e => e.Id);

    public ItemPlace PlaceOf(Entity player)
    {
        if (MazeOf(player) is not { } maze)
        {
            return ItemPlace.District;
        }

        var itemsAllowed = !(maze.Maze > 0 && _rules.TryGet((ushort)maze.Maze, out var rule) && rule.PotionLimit == 1);
        var infinite = _mazes.TryGet(maze.Maze, out var info) && info.Type == InfiniteTowerType;

        return new ItemPlace(true, itemsAllowed, true, !maze.Creatures.Vitals.IsAlive(player), infinite);
    }

    public bool GiveBuff(Entity player, int buff)
    {
        return MazeOf(player) is { } maze && maze.Buffs.Give(player, buff, owner: 0, roll: false);
    }

    private LuaMaze? MazeOf(Entity player)
    {
        return world.IsAlive(player) && world.TryGet(player, out LuaMaze? maze) && maze is { IsClosed: false } && maze.Members.Contains(player) ? maze : null;
    }
}
