using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.League;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Maze.Responses;

namespace OpenWorker.Gameplay.Modules.Drops;

/// <summary>
/// What a player has got in the maze it is in, told to it in the window that is shown when the maze is finished.
/// The window shows what was really given, there is no scoring behind the rank yet: a maze that is finished by clearing
/// all its sectors has the highest rank. The window also offers the key reward of the maze (<see cref="MazeKeyRewards"/>).
/// The league of the player hears of the run when it starts and when it ends (<see cref="LeagueGrowth"/>).
/// </summary>
public sealed class MazeResults(World world, MazeKeyRewards? keys = null, LeagueGrowth? league = null) : ISessionCloseHandler
{
    public const byte BestRank = 5;

    private sealed class Tally(int maze)
    {
        public int Maze { get; } = maze;
        public long Started { get; } = Environment.TickCount64;
        public int Kills { get; set; }
        public int Experience { get; set; }
        public int Gold { get; set; }
        public int Ether { get; set; }
    }

    private readonly Dictionary<Entity, Tally> _tallies = [];

    public void Start(Entity player, int maze)
    {
        _tallies[player] = new Tally(maze);
        keys?.Forget(player);
        league?.OnMazeEntered(player, maze);
    }

    public void AddKill(Entity player, int experience)
    {
        if (_tallies.TryGetValue(player, out var tally))
        {
            tally.Kills++;
            tally.Experience += experience;
        }
    }

    public void AddGold(Entity player, int gold)
    {
        if (_tallies.TryGetValue(player, out var tally))
        {
            tally.Gold += gold;
        }
    }

    public void AddEther(Entity player, int ether)
    {
        if (_tallies.TryGetValue(player, out var tally))
        {
            tally.Ether += ether;
        }
    }

    /// <summary>
    /// The maze is finished: shows the player how it went. Only once for a maze.
    /// </summary>
    public void Complete(Entity player)
    {
        if (!_tallies.Remove(player, out var tally) || !world.Has<ServerSessionComponent>(player))
        {
            return;
        }

        var session = world.Get<ServerSessionComponent>(player);
        var seconds = (int)Math.Min(int.MaxValue, (Environment.TickCount64 - tally.Started) / 1000);
        var offer = keys?.Roll(player, tally.Maze);

        league?.OnMazeFinished(player, tally.Maze, tally.Gold);

        session.Send(new MazeRewardInfoResponse
        {
            Rank = BestRank,
            ClearTime = seconds,
            TotalCombo = tally.Kills,
            PlayExp = tally.Experience,
            TotalExp = tally.Experience,
            PlayGold = tally.Gold,
            TotalMoney = tally.Gold,
            TotalEther = tally.Ether,
            CashItemCount = offer?.Count ?? 0,
            CashItem = offer?.Item ?? 0
        });

        session.Send(new MazeRewardPartyInfoResponse
        {
            Members =
            [
                new MazeRewardPartyMember(1, BestRank, world.Get<ActorComponent>(player).Identifier, world.Get<PersonInfoComponent>(player).Name, 0)
            ]
        });
    }

    public ValueTask OnSessionClosedAsync(Entity player)
    {
        _tallies.Remove(player);
        keys?.Forget(player);

        return ValueTask.CompletedTask;
    }
}
