using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Maze;

/// <summary>
/// The revive items of a maze: a call a fallen player uses to get up. The maze server says what gets a player up
/// (<see cref="MazeRevival"/> only decides whether it may).
/// </summary>
public interface IReviveItems
{
    /// <summary>
    /// The player uses the revive item. Used when the item is spent and the player is up; else the refusal with its code.
    /// </summary>
    ItemUseResult Use(Entity player, ItemInstance item);
}

/// <summary>
/// When a fallen player may get up in a maze, by the rules of the reference:
/// <list type="bullet">
/// <item>Not at all in a maze that does not let anybody (<c>RevivalType</c> 1): <see cref="NotInThisMaze"/>.</item>
/// <item>Only a fallen player gets up: <see cref="NotFallen"/>.</item>
/// <item>By the buff of another player, which nothing here casts: <see cref="NotAllowed"/>. By the safe place of a district,
/// which is none in a maze: <see cref="NotAllowed"/>.</item>
/// <item>Free, <see cref="MazeEntryLimits.FreeRevivesPerDay"/> times a day: <see cref="NoFreeRevive"/> after that.</item>
/// <item>For cash: there is no cash shop here, so <see cref="NoFreeRevive"/>.</item>
/// <item>By the maze itself in the maze of the mode that revives its players: <see cref="NotInThisMode"/> elsewhere.</item>
/// <item>By an item: as many as the maze allows (<c>RevivalItemCount</c>, none for as many as the player has):
/// <see cref="ItemsOver"/> after that.</item>
/// </list>
/// A server without the table of the mazes lets everybody get up.
/// </summary>
public sealed class MazeRevival(ReadOnlyCollection<MazeRuleRow> rules, MazeEntryLimits limits) : ISessionCloseHandler
{
    /// <summary>The maze does not let a player get up.</summary>
    public const ushort NotInThisMaze = 52617;

    /// <summary>The player has not fallen.</summary>
    public const ushort NotFallen = 52608;

    /// <summary>The way the player asks to get up is not open to it here.</summary>
    public const ushort NotAllowed = 51009;

    /// <summary>The player has no free revive left, or no way to pay.</summary>
    public const ushort NoFreeRevive = 51014;

    /// <summary>The maze is not of the mode that revives its players.</summary>
    public const ushort NotInThisMode = 51010;

    /// <summary>The player has used all the revive items the maze allows (the error of the use of an item).</summary>
    public const ushort ItemsOver = 52606;

    /// <summary>The kind of maze of the mode that revives its players by itself.</summary>
    private const byte ModeMazeKind = 12;

    private const byte Impossible = 1;
    private const byte Unlimited = 2;

    private readonly RowIndex<ushort, MazeRuleRow> _rules = rules.IndexBy(e => e.Id);
    private readonly Dictionary<Entity, int> _itemsUsed = [];

    /// <summary>
    /// Whether the player may get up the way it asks. Null when it may, else the code of the refusal. A free revive is used
    /// up by the check that lets it through.
    /// </summary>
    public ushort? Check(Entity player, int maze, bool fallen, ReviveType type)
    {
        _rules.TryGet((ushort)Math.Clamp(maze, 0, ushort.MaxValue), out var rule);

        if (rule.RevivalType == Impossible)
        {
            return NotInThisMaze;
        }

        if (!fallen)
        {
            return NotFallen;
        }

        switch (type)
        {
            case ReviveType.MazeFree:
                return limits.TryUseFreeRevive(player) ? null : NoFreeRevive;

            case ReviveType.ModeMazeAuto:
                return rule.Type == ModeMazeKind && rule.RevivalType == Unlimited ? null : NotInThisMode;

            case ReviveType.Cash:
                return NoFreeRevive;

            // The buff of another player, the safe place of a district, and the way of the server itself.
            default:
                return NotAllowed;
        }
    }

    /// <summary>
    /// Whether the player may use a revive item now. Null when it may, else the code of the refusal.
    /// </summary>
    public ushort? CheckItem(Entity player, int maze, bool fallen)
    {
        _rules.TryGet((ushort)Math.Clamp(maze, 0, ushort.MaxValue), out var rule);

        if (rule.RevivalType == Impossible)
        {
            return NotInThisMaze;
        }

        if (!fallen)
        {
            return NotFallen;
        }

        return rule.RevivalItemCount != 0 && _itemsUsed.GetValueOrDefault(player) >= rule.RevivalItemCount ? ItemsOver : null;
    }

    /// <summary>
    /// The player used a revive item in the maze it is in.
    /// </summary>
    public void ItemUsed(Entity player)
    {
        _itemsUsed[player] = _itemsUsed.GetValueOrDefault(player) + 1;
    }

    public ValueTask OnSessionClosedAsync(Entity player)
    {
        _itemsUsed.Remove(player);

        return ValueTask.CompletedTask;
    }
}
