using Arch.Core;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Modules.Drops;
using OpenWorker.Gameplay.Modules.Rewards;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Rewards.Types;
using OpenWorker.Hotspot.Modules.Vaccum.Responses;
using OpenWorker.Hotspot.Modules.Vaccum.Types;

namespace OpenWorker.Gameplay.Modules.Vaccum;

/// <summary>
/// A cube a person can take: the click is held for a while, and each time it is held to the end the cube gives something, until
/// the picks of the cube are used up.
/// </summary>
public sealed class VaccumCubeState(int cube, int value, TimeSpan hold, int picks)
{
    public int Cube { get; } = cube;

    /// <summary>
    /// What decides the reward, the row of the table the maze names for the cube.
    /// </summary>
    public int Value { get; } = value;

    /// <summary>
    /// How long the click is held for one pick.
    /// </summary>
    public TimeSpan Hold { get; } = hold;

    /// <summary>
    /// How many times the cube can still be taken.
    /// </summary>
    public int Picks { get; set; } = picks;
}

/// <summary>
/// What one pick of a cube gives: an item or a kind of money (<see cref="Item"/>, <see cref="Count"/>), nothing at all, or a refusal
/// the table itself makes (<see cref="Error"/>).
/// </summary>
public readonly record struct VaccumPick(int Item, short Count, ushort Error);

/// <summary>
/// How long a cube is held, and how many picks a new one has, at the fewest and at the most.
/// </summary>
public readonly record struct VaccumCubeRule(TimeSpan Hold, int FewestPicks, int MostPicks);

/// <summary>
/// What a cube asks and gives, which the tables of the server tell.
/// </summary>
public interface IVaccumRewards
{
    /// <summary>
    /// What a pick of the cube of that object gives.
    /// </summary>
    VaccumPick Roll(int interaction);

    /// <summary>
    /// How the cube of that object is held and how often it is taken, or nothing when the tables do not know it.
    /// </summary>
    VaccumCubeRule? RuleOf(int interaction);
}

/// <summary>
/// Cubes lie in mazes. A person clicks one and has to stay with it for a while; if the click stops, the cube is free again,
/// if it holds out, the cube gives its pick and is free again, and is gone when its last pick is taken. The maze tells the manager
/// which cubes there are.
/// </summary>
public sealed partial class VaccumManager(
    World world,
    IRewardGranter rewards,
    IVaccumRewards cubeRewards,
    ILogger<VaccumManager> logger)
{
    /// <summary>
    /// The cube is not there, or was let go.
    /// </summary>
    public const int Released = 55800;

    /// <summary>
    /// The person is already taking a cube.
    /// </summary>
    public const int Busy = 55801;

    /// <summary>
    /// A cancel of a cube the person was not holding.
    /// </summary>
    public const int NotHeld = 55802;

    /// <summary>
    /// The gold of the pick did not fit in the purse.
    /// </summary>
    public const int NoRoomForGold = 52056;

    /// <summary>
    /// The battle points of the pick did not fit in the purse.
    /// </summary>
    public const int NoRoomForBattlePoints = 52057;

    /// <summary>
    /// The ether of the pick did not fit in the purse.
    /// </summary>
    public const int NoRoomForEther = 52058;

    /// <summary>
    /// The item of the pick did not fit in the bags.
    /// </summary>
    public const int NoRoomForItems = 52010;

    /// <summary>
    /// Puts a cube in the world of the person and tells the client. A cube the tables know is held for the time they give and has
    /// as many picks as they say, at random; any other is held for <paramref name="hold"/> and taken once.
    /// </summary>
    public void Register(Entity player, int cube, int value, TimeSpan hold)
    {
        if (!world.IsAlive(player))
        {
            return;
        }

        var component = Get(player);
        var rule = cubeRewards.RuleOf(value);
        var picks = rule is { } known ? Random.Shared.Next(Math.Max(1, known.FewestPicks), Math.Max(1, known.MostPicks) + 1) : 1;

        component.Cubes ??= [];
        component.Cubes[cube] = new VaccumCubeState(cube, value, rule?.Hold ?? hold, picks);

        world.Set(player, component);

        world.Get<ServerSessionComponent>(player)
            .Send(new VaccumInResponse { Cube = cube, Value = value, State = 0 });
    }

    /// <summary>
    /// Tells the client about all the cubes the person has.
    /// </summary>
    public void SendAll(Entity player)
    {
        var cubes = Get(player).Cubes;

        if (cubes is null || cubes.Count == 0)
        {
            return;
        }

        world.Get<ServerSessionComponent>(player)
            .Send(new VaccumInfosResponse
            {
                Cubes = cubes.Values.Select(e => new VaccumCubeValue(e.Cube, e.Value, 0)).ToArray()
            });
    }

    /// <summary>
    /// The person clicks a cube. A person who is taking one already, or a cube that is not there, is refused with the code of the
    /// game.
    /// </summary>
    public void ClickStart(Entity player, int cube)
    {
        var component = Get(player);
        var session = world.Get<ServerSessionComponent>(player);
        var actor = world.Get<ActorComponent>(player);

        if (component.Taking != 0)
        {
            session.Send(new VaccumClickStartResponse { Actor = actor, Cube = cube, Result = Busy });

            return;
        }

        if (component.Cubes is null || !component.Cubes.TryGetValue(cube, out var state))
        {
            session.Send(new VaccumClickStartResponse { Actor = actor, Cube = cube, Result = Released });

            return;
        }

        component.Taking = cube;
        component.Ticket++;

        world.Set(player, component);

        session.Send(new VaccumClickStartResponse { Actor = actor, Cube = cube, Result = 0 });

        _ = CompleteAfterAsync(player, state, component.Ticket);
    }

    /// <summary>
    /// The person lets the cube go. A cube that is not there, or that the person is not holding, is answered with the code of the
    /// game.
    /// </summary>
    public void ClickCancel(Entity player, int cube)
    {
        var component = Get(player);
        var session = world.Get<ServerSessionComponent>(player);
        var actor = world.Get<ActorComponent>(player);

        if (component.Cubes is null || !component.Cubes.ContainsKey(cube))
        {
            session.Send(new VaccumClickCancelResponse { Actor = actor, Cube = cube, Result = Released });

            return;
        }

        if (component.Taking != cube)
        {
            session.Send(new VaccumClickCancelResponse { Actor = actor, Cube = cube, Result = NotHeld });

            return;
        }

        component.Taking = 0;
        component.Ticket++;

        world.Set(player, component);

        session.Send(new VaccumClickCancelResponse { Actor = actor, Cube = cube, Result = Released });
    }

    // Waits on the world loop, so what follows can use the world. A take that was cancelled or replaced finds a different ticket.
    private async Task CompleteAfterAsync(Entity player, VaccumCubeState state, int ticket)
    {
        try
        {
            await Task.Delay(state.Hold);

            if (!world.IsAlive(player))
            {
                return;
            }

            var component = Get(player);

            if (component.Ticket != ticket || component.Taking != state.Cube)
            {
                return;
            }

            var pick = cubeRewards.Roll(state.Value);
            var reward = RewardOf(pick);
            var error = pick.Error;

            if (error == 0 && !reward.IsEmpty && !await rewards.TryGrantAsync(player, reward, RewardSource.Vaccum, CancellationToken.None))
            {
                error = (ushort)ErrorOf(pick);
            }

            if (!world.IsAlive(player))
            {
                return;
            }

            component = Get(player);
            component.Taking = 0;
            component.Ticket++;

            // A pick that went wrong is not used up.
            if (error == 0)
            {
                state.Picks--;
            }

            var gone = error == 0 && state.Picks <= 0;

            if (gone)
            {
                component.Cubes?.Remove(state.Cube);
            }

            world.Set(player, component);

            var session = world.Get<ServerSessionComponent>(player);
            var actor = world.Get<ActorComponent>(player);

            session.Send(new VaccumClickCompleteResponse
            {
                Actor = actor,
                Cube = state.Cube,
                Remain = (short)Math.Max(0, state.Picks),
                Items = error == 0 && pick.Item != 0 ? [new RewardItemValue(pick.Item, pick.Count)] : [],
                Result = error
            });

            // The cube is let go whatever came of the pick.
            session.Send(new VaccumClickCancelResponse { Actor = actor, Cube = state.Cube, Result = Released });

            if (gone)
            {
                session.Send(new VaccumOutResponse { Cube = state.Cube, Flag = 0 });
            }
        }

        catch (Exception e) when (e is not OperationCanceledException)
        {
            LogTakeFailed(e);
        }
    }

    /// <summary>
    /// A pick as something to give: money goes to the purse, anything else to the bags.
    /// </summary>
    private static Reward RewardOf(VaccumPick pick)
    {
        return pick.Item switch
        {
            0 => Reward.Empty,
            DropService.GoldObject => new Reward([], Gold: pick.Count),
            DropService.EtherObject => new Reward([], Ether: pick.Count),
            DropService.BattlePointObject => new Reward([], BattlePoints: pick.Count),
            _ => new Reward([new RewardItem(pick.Item, pick.Count)])
        };
    }

    /// <summary>
    /// What the client is told when the pick did not fit: the purse or the bags that were full.
    /// </summary>
    private static int ErrorOf(VaccumPick pick)
    {
        return pick.Item switch
        {
            DropService.GoldObject => NoRoomForGold,
            DropService.EtherObject => NoRoomForEther,
            DropService.BattlePointObject => NoRoomForBattlePoints,
            _ => NoRoomForItems
        };
    }

    private VaccumComponent Get(Entity player) =>
        world.IsAlive(player) && world.TryGet<VaccumComponent>(player, out var component) ? component : default;

    [LoggerMessage(Level = LogLevel.Error, Message = "Taking a cube failed.")]
    private partial void LogTakeFailed(Exception exception);
}
