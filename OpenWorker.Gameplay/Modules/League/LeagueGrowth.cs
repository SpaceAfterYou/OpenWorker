using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Modules.League.Components;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.League;

/// <summary>
/// What a member brings to the league: experience and gold, and the member's own share of the experience.
/// </summary>
public readonly record struct LeagueWealth(int League, int Person, int Exp, long Gold);

/// <summary>
/// Where the league keeps what its members bring, and who hears that a member reached a level. The league lives in the
/// database and its members on every server, so the work behind it does not wait in the game.
/// </summary>
public interface ILeagueTreasury
{
    void Add(LeagueWealth wealth);

    void LevelReached(int league, int person, byte level);
}

/// <summary>
/// How a league grows from what its members do. A member who enters a maze that costs fatigue brings half of that cost as
/// experience; the member who finishes it brings the other half, and a hundredth of the gold the run gave. A member who
/// reaches a level shows it to the others.
/// </summary>
public sealed class LeagueGrowth(World world, ReadOnlyCollection<MazeInfoRow> mazes, ILeagueTreasury treasury)
{
    /// <summary>
    /// The share of the gold of a run that goes to the league, in percent.
    /// </summary>
    public const int GoldSharePercent = 1;

    private readonly RowIndex<int, MazeInfoRow> _mazes = mazes.IndexBy(e => (int)e.Id);

    /// <summary>
    /// The experience a member brings at each end of a run: half the fatigue the maze costs.
    /// </summary>
    public int ExpOf(int maze)
    {
        return _mazes.TryGet(maze, out var row) && row.FatiguePoint > 1 ? row.FatiguePoint / 2 : 0;
    }

    /// <summary>
    /// The person has entered a maze.
    /// </summary>
    public void OnMazeEntered(Entity player, int maze)
    {
        Bring(player, ExpOf(maze), 0);
    }

    /// <summary>
    /// The person has finished a maze, with the gold the run gave.
    /// </summary>
    public void OnMazeFinished(Entity player, int maze, long gold)
    {
        Bring(player, ExpOf(maze), Math.Max(0, gold) * GoldSharePercent / 100);
    }

    /// <summary>
    /// The person has reached a level.
    /// </summary>
    public void OnLevelReached(Entity player, byte level)
    {
        if (LeagueOf(player) is var (league, person))
        {
            treasury.LevelReached(league, person, level);
        }
    }

    private void Bring(Entity player, int exp, long gold)
    {
        if ((exp > 0 || gold > 0) && LeagueOf(player) is var (league, person))
        {
            treasury.Add(new LeagueWealth(league, person, exp, gold));
        }
    }

    private (int League, int Person)? LeagueOf(Entity player)
    {
        if (!world.IsAlive(player) || !world.TryGet<LeagueComponent>(player, out var league) || league.Id <= 0 || !world.Has<ActorComponent>(player))
        {
            return null;
        }

        return (league.Id, world.Get<ActorComponent>(player).Identifier);
    }
}
