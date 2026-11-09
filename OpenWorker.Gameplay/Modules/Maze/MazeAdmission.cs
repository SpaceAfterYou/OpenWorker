using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Gameplay.Modules.Drops;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Progress;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Maze;

/// <summary>
/// What a maze asks of whoever takes its portal, checked in the order of the reference: who may come (alone, a party),
/// the level, the maze cleared before it, the fatigue. Each refusal is the error of the request with the code the
/// client knows; a member of a party is checked the same way before the party is asked.
/// </summary>
public sealed class MazeAdmission(
    World world,
    ReadOnlyCollection<MazeInfoRow> mazes,
    FatigueService fatigue,
    MazeScoring scoring,
    SharePointManager progress)
{
    /// <summary>The maze is for a player alone.</summary>
    public const ushort AloneOnly = 55032;

    /// <summary>The maze is for a party.</summary>
    public const ushort PartyOnly = 55033;

    /// <summary>Nobody enters the maze this way.</summary>
    public const ushort Closed = 55052;

    /// <summary>The level of the player is below the level of the maze.</summary>
    public const ushort LevelTooLow = 55012;

    /// <summary>The level of a member of the party is below the level of the maze.</summary>
    public const ushort MemberLevelTooLow = 55013;

    /// <summary>The maze before it is not cleared.</summary>
    public const ushort NotOpened = 55003;

    /// <summary>A member of the party has not cleared the maze before it.</summary>
    public const ushort MemberNotOpened = 55048;

    private const byte Anybody = 0;
    private const byte Alone = 1;
    private const byte Party = 2;
    private const byte FullParty = 3;

    private readonly RowIndex<short, MazeInfoRow> _mazes = mazes.IndexBy(e => e.Id);

    /// <summary>
    /// A player alone takes the portal of a maze. Null when it may enter, else the code of the refusal; a location that is no
    /// maze of the tables is not this check's to refuse.
    /// </summary>
    public ushort? Check(Entity player, short maze)
    {
        if (!_mazes.TryGet(maze, out var row))
        {
            return null;
        }

        if (row.Admission is Party or FullParty)
        {
            return PartyOnly;
        }

        if (row.Admission is not (Anybody or Alone))
        {
            return Closed;
        }

        return CheckPerson(player, row, LevelTooLow, NotOpened);
    }

    /// <summary>
    /// The master of a party takes the portal of a maze for the party: the maze must take a party, and every member must be
    /// able to enter. Null when they may, else the code of the refusal for the master.
    /// </summary>
    public ushort? CheckParty(IReadOnlyList<Entity> members, short maze)
    {
        if (!_mazes.TryGet(maze, out var row))
        {
            return null;
        }

        if (row.Admission == Alone)
        {
            return AloneOnly;
        }

        if (row.Admission is not (Anybody or Party or FullParty))
        {
            return Closed;
        }

        foreach (var member in members)
        {
            if (CheckPerson(member, row, MemberLevelTooLow, MemberNotOpened) is { } refused)
            {
                return refused;
            }
        }

        return null;
    }

    private ushort? CheckPerson(Entity player, in MazeInfoRow row, ushort levelTooLow, ushort notOpened)
    {
        if (!world.IsAlive(player))
        {
            return null;
        }

        var level = world.TryGet<LevelComponent>(player, out var rank) ? rank.Level : 0;

        if (row.RequiredLevel > 0 && level < row.RequiredLevel)
        {
            return levelTooLow;
        }

        // The maze before it is named only in the server's own row of the maze.
        if (scoring.TryGetRule(row.Id, out var rule) && rule.RequiredMaze is > 0 and <= int.MaxValue &&
            !progress.HasCleared(player, (int)rule.RequiredMaze))
        {
            return notOpened;
        }

        if (!fatigue.CanEnter(player, row.Id))
        {
            return FatigueService.NotEnough;
        }

        return null;
    }
}
