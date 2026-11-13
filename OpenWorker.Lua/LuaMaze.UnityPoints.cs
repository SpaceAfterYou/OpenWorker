using Arch.Core;
using Lua;
using Microsoft.Extensions.Logging;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Gameplay.Modules.UnityPoints;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Maze.Responses;

namespace OpenWorker.Lua;

/// <summary>
/// The unity mode: while it runs, every combo of a player that ends gives that player battle points, shown on its panel
/// (11/59) and paid into its purse at the clear, or when it leaves the maze before. A player who falls is told the mode is lost
/// for it; when nobody stands, the mode ends. No stage of the event gives D.S points yet.
/// </summary>
public partial class LuaMaze
{
    private const int UnityStarted = 31;
    private const int UnityEnded = 32;

    // The value of the end notice the client shows as a loss.
    private const int UnityLost = 1;

    private readonly Dictionary<Entity, UnityTally> _unity = [];

    /// <summary>
    /// The unity events a script can start, or nothing for a maze that starts none.
    /// </summary>
    public UnityEvents? UnityEvents { get; init; }

    /// <summary>
    /// The script starts the unity event of the table of that number in the sector. A number the table has no row for starts
    /// nothing.
    /// </summary>
    [LuaIgnoreMember]
    internal void BeginUnity(int table, int sector)
    {
        if (Closed || UnityEvents is not { } events || !events.Has(table))
        {
            Logger.UnknownMode(nameof(BeginUnity), table);

            return;
        }

        LogUnityStarted(Logger, table, sector);

        Begin(new UnityMode(this, (long)Clock.Time.TotalMilliseconds));

        Notice(UnityStarted, 1);
    }

    /// <summary>
    /// A player whose client has loaded the maze while the unity mode runs gets its panel, with what it has earned.
    /// </summary>
    private void TellUnity(Entity player)
    {
        if (_mode is not UnityMode || !TrySession(player, out var session))
        {
            return;
        }

        session.Send(new MazeNoticeResponse { Type = UnityStarted, Value = 1 });

        if (_unity.TryGetValue(player, out var tally))
        {
            session.Send(UnityMessage(tally));
        }
    }

    /// <summary>
    /// The battle points the player has earned in the maze go into its purse.
    /// </summary>
    private void PayUnityPoints(Entity player)
    {
        if (UnityEvents is { } events && _unity.Remove(player, out var tally) && Ecs.IsAlive(player) && Ecs.Has<CurrencyComponent>(player))
        {
            events.Pay(player, tally);
        }
    }

    /// <summary>
    /// The maze is cleared: everyone in it is paid the battle points they have earned.
    /// </summary>
    private void PayUnityPoints()
    {
        foreach (var player in _unity.Keys.ToArray())
        {
            PayUnityPoints(player);
        }
    }

    private static MazeUnityPointResponse UnityMessage(UnityTally tally)
    {
        return new MazeUnityPointResponse { BattlePoints = tally.BattlePoints, DsPoints = 0 };
    }

    private sealed class UnityMode(LuaMaze maze, long since) : GameMode(maze)
    {
        private readonly HashSet<Entity> _fallen = [];

        /// <summary>
        /// The combos that are over count for their players, each told its points.
        /// </summary>
        public override void Tick(float seconds)
        {
            if (Maze.Closed || Maze.IsComplete)
            {
                return;
            }

            var now = (long)Maze.Clock.Time.TotalMilliseconds;

            foreach (var member in Maze._members)
            {
                if (member.Gone || !Maze.Ecs.IsAlive(member.Player))
                {
                    continue;
                }

                if (!Maze._unity.TryGetValue(member.Player, out var tally))
                {
                    tally = new UnityTally();
                    Maze._unity[member.Player] = tally;
                }

                if (tally.CountCombo(member.Combo, member.LastHit, now, since) && member.Loaded &&
                    Maze.TrySession(member.Player, out var session))
                {
                    session.Send(UnityMessage(tally));
                }
            }
        }

        /// <summary>
        /// A player who has fallen is told the mode is lost for it; the mode is over when nobody stands.
        /// </summary>
        public override ValueTask<bool> CheckEndAsync()
        {
            if (Maze.Closed || Maze._members.Count == 0)
            {
                return ValueTask.FromResult(false);
            }

            foreach (var member in Maze._members)
            {
                var player = member.Player;

                if (!Maze.Ecs.IsAlive(player))
                {
                    continue;
                }

                if (Maze.Creatures.Vitals.IsAlive(player))
                {
                    _fallen.Remove(player);
                }
                else if (_fallen.Add(player) && member.Loaded && Maze.TrySession(player, out var session))
                {
                    session.Send(new MazeNoticeResponse { Type = UnityEnded, Value = UnityLost });
                }
            }

            return ValueTask.FromResult(!Maze.AnyoneStands());
        }
    }

    /// <summary>
    /// The combos of the players give battle points until the maze is cleared.
    /// </summary>
    [LuaMember]
    public void StartUnityMode(int table, int sector)
    {
        BeginUnity(table, sector);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Unity mode {Table} started in sector {Sector}")]
    private static partial void LogUnityStarted(ILogger logger, int table, int sector);
}
