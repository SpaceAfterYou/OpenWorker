using System.Collections.ObjectModel;
using Arch.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Modules.Boosters;
using OpenWorker.Gameplay.Modules.DailyMissions;
using OpenWorker.Gameplay.Modules.Maze.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Maze;

/// <summary>
/// The fatigue of a person, which mazes spend. It is kept in the row of the person (<see cref="FatiguePointsComponent"/>,
/// written by the flush with the wallet) and told to the client by 03/64.
/// <list type="bullet">
/// <item>A person has up to <see cref="MaxCommon"/> points of the day and up to <see cref="MaxBonus"/> saved up; a maze
/// spends the saved points first.</item>
/// <item>Entering a maze asks for its whole cost (<see cref="CanEnter"/>, refused with <see cref="NotEnough"/>); half of it
/// is spent when the player comes into the maze and half at the clear, each half less half of what the boosters take
/// off.</item>
/// <item>At the start of each game day (the day of the daily missions) the points of the day fill up and half of what was
/// left of them, at most <see cref="DailyBonus"/>, is saved up, with <see cref="DailyBonus"/> more for each whole day the
/// person was away, at most <see cref="MaxBonusDays"/> days.</item>
/// <item>An item of fatigue adds its value to the points of the day, unless the person has saved points or a full day
/// (<see cref="CannotRestore"/>).</item>
/// </list>
/// </summary>
public sealed partial class FatigueService(
    World world,
    ReadOnlyCollection<MazeInfoRow> mazes,
    TimeProvider time,
    IOptions<DailyMissionOptions> options,
    ILogger<FatigueService> logger,
    BoosterManager? boosters = null) : IMiscTick
{
    public const short MaxCommon = 200;
    public const short MaxBonus = 400;

    /// <summary>The most a day saves up of what was left, and what each whole day away adds.</summary>
    public const short DailyBonus = 100;

    public const int MaxBonusDays = MaxBonus / DailyBonus;

    /// <summary>The person has too little fatigue for the maze (the error of 04/01).</summary>
    public const ushort NotEnough = 55044;

    /// <summary>The person has saved points or a full day: an item of fatigue is refused (the error of 08/11).</summary>
    public const ushort CannotRestore = 52611;

    private static QueryDescription Persons { get; } = new QueryDescription().WithAll<FatiguePointsComponent, ServerSessionComponent>();

    private readonly RowIndex<short, MazeInfoRow> _mazes = mazes.IndexBy(e => e.Id);

    private TimeSpan Reset => options.Value.ResetHour is { } hour ? TimeSpan.FromHours(hour) : DailyMissionRules.DefaultResetTime;

    /// <summary>
    /// What the maze costs to enter; 0 for a maze the tables do not have.
    /// </summary>
    public int CostOf(int maze)
    {
        return maze is > 0 and <= short.MaxValue && _mazes.TryGet((short)maze, out var row) ? Math.Max((short)0, row.FatiguePoint) : 0;
    }

    /// <summary>
    /// Whether the person has the fatigue to enter the maze: its whole cost, less what the boosters take off.
    /// </summary>
    public bool CanEnter(Entity player, int maze)
    {
        var cost = CostOf(maze);

        if (cost <= 0)
        {
            return true;
        }

        if (!Refresh(player, out var fatigue))
        {
            return false;
        }

        cost = Math.Max(0, cost - (int)(boosters?.GetValue(player, BoosterEffect.FatigueDecrease) ?? 0));

        return fatigue.Common + fatigue.Bonus >= cost;
    }

    /// <summary>
    /// Spends half the cost of the maze, as the player comes into it and again at its clear. A maze that costs one or
    /// nothing spends nothing on the way in.
    /// </summary>
    public void SpendHalf(Entity player, int maze, bool entering)
    {
        var cost = CostOf(maze);

        if (cost <= 0 || (entering && cost <= 1))
        {
            return;
        }

        var half = cost / 2 - (int)(boosters?.GetValue(player, BoosterEffect.FatigueDecrease) ?? 0) / 2;

        if (half > 0)
        {
            Spend(player, half);
        }
    }

    /// <summary>
    /// Spends fatigue, the saved points first; what is not there is not spent.
    /// </summary>
    public void Spend(Entity player, int amount)
    {
        if (amount <= 0 || !Refresh(player, out var fatigue))
        {
            return;
        }

        var fromBonus = Math.Min(amount, (int)fatigue.Bonus);
        var fromCommon = Math.Min(amount - fromBonus, (int)fatigue.Common);

        Set(player, fatigue with { Bonus = (short)(fatigue.Bonus - fromBonus), Common = (short)(fatigue.Common - fromCommon) });
        LogSpent(logger, amount, fatigue.Common - fromCommon, fatigue.Bonus - fromBonus);
    }

    /// <summary>
    /// An item of fatigue adds its value to the points of the day. Null when it is added, else the code of the refusal.
    /// </summary>
    public ushort? Restore(Entity player, int amount)
    {
        if (amount <= 0 || !Refresh(player, out var fatigue) || fatigue.Bonus > 0 || fatigue.Common >= MaxCommon)
        {
            return CannotRestore;
        }

        Set(player, fatigue with { Common = (short)Math.Min(MaxCommon, fatigue.Common + amount) });

        return null;
    }

    /// <summary>
    /// The fatigue of the person now, filled up first when a new game day has begun since the last time.
    /// </summary>
    public bool Refresh(Entity player, out FatiguePointsComponent fatigue)
    {
        if (!world.IsAlive(player) || !world.TryGet(player, out fatigue))
        {
            fatigue = default;
            return false;
        }

        var now = time.GetUtcNow();
        var day = GameDay.Get(now, Reset).Start.UtcDateTime;

        // A person that was never filled starts its days now, with what it was made with.
        if (fatigue.RefilledAt is not { } last)
        {
            fatigue = fatigue with { RefilledAt = now.UtcDateTime };
            world.Set(player, fatigue);
            world.MarkDirty(player, PersonSections.Currencies);

            return true;
        }

        if (last >= day)
        {
            return true;
        }

        var saved = Math.Min(DailyBonus, (int)(fatigue.Common * 0.5f));
        var away = (int)Math.Min(MaxBonusDays, Math.Max(0, (day - last).TotalDays));

        saved = Math.Min(MaxBonus, saved + DailyBonus * away);

        fatigue = fatigue with
        {
            Common = MaxCommon,
            Bonus = (short)Math.Min(MaxBonus, fatigue.Bonus + saved),
            RefilledAt = now.UtcDateTime
        };

        Set(player, fatigue);
        LogRefilled(logger, saved, fatigue.Bonus);

        return true;
    }

    /// <summary>
    /// Fills up the fatigue of every person whose game day has turned.
    /// </summary>
    public ValueTask TickAsync(CancellationToken cancellationToken)
    {
        var players = new List<Entity>();

        world.Query(Persons, entity => players.Add(entity));

        foreach (var player in players)
        {
            Refresh(player, out _);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Tells the client the fatigue of the person.
    /// </summary>
    public void Send(Entity player)
    {
        if (world.IsAlive(player) && world.TryGet<FatiguePointsComponent>(player, out var fatigue) && world.Has<ServerSessionComponent>(player))
        {
            world.Get<ServerSessionComponent>(player).Send(new PersonFpUpdateResponse { Common = fatigue.Common, Bonus = fatigue.Bonus });
        }
    }

    private void Set(Entity player, FatiguePointsComponent fatigue)
    {
        world.Set(player, fatigue);
        world.MarkDirty(player, PersonSections.Currencies);
        Send(player);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fatigue {Amount} spent, {Common} and {Bonus} saved left")]
    private static partial void LogSpent(ILogger logger, int amount, int common, int bonus);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fatigue filled for a new day, {Saved} saved, {Bonus} saved in all")]
    private static partial void LogRefilled(ILogger logger, int saved, int bonus);
}
