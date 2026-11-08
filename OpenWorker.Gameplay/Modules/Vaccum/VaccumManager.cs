using Arch.Core;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Modules.Rewards;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Rewards.Types;
using OpenWorker.Hotspot.Modules.Vaccum.Responses;
using OpenWorker.Hotspot.Modules.Vaccum.Types;

namespace OpenWorker.Gameplay.Modules.Vaccum;

/// <summary>
/// A cube a person can take: it has to be held for a while, and then gives something.
/// </summary>
public sealed class VaccumCubeState(int cube, int value, TimeSpan duration)
{
    public int Cube { get; } = cube;

    /// <summary>
    /// What decides the reward, the row of the table the maze names for the cube.
    /// </summary>
    public int Value { get; } = value;

    public TimeSpan Duration { get; } = duration;
}

/// <summary>
/// What a cube gives. The table that tells is not part of the client, so whoever has it provides this.
/// </summary>
public interface IVaccumRewards
{
    Reward Roll(int value);
}

/// <summary>
/// Used until the table is there: cubes give nothing.
/// </summary>
public sealed class NoVaccumRewards : IVaccumRewards
{
    public Reward Roll(int value) => Reward.Empty;
}

/// <summary>
/// Cubes lie in mazes. A person clicks one and has to stay with it for a while; if it stops, the cube is free again,
/// if it holds out, the cube gives its reward and is gone. The maze tells the manager which cubes there are.
/// </summary>
public sealed partial class VaccumManager(
    World world,
    IRewardGranter rewards,
    IVaccumRewards cubeRewards,
    ILogger<VaccumManager> logger)
{
    /// <summary>
    /// The cube was let go.
    /// </summary>
    public const int Released = 55800;

    /// <summary>
    /// The person is already taking a cube. The code is the one next to the others of the cubes, not read from anywhere.
    /// </summary>
    public const int Busy = 55801;

    /// <summary>
    /// The reward could not be given.
    /// </summary>
    public const int CannotTake = 52056;

    /// <summary>
    /// Puts a cube in the world of the person and tells the client.
    /// </summary>
    public void Register(Entity player, int cube, int value, TimeSpan duration)
    {
        if (!world.IsAlive(player))
        {
            return;
        }

        var component = Get(player);

        component.Cubes ??= [];
        component.Cubes[cube] = new VaccumCubeState(cube, value, duration);

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
    /// The person clicks a cube. A cube that is not there is ignored.
    /// </summary>
    public void ClickStart(Entity player, int cube)
    {
        var component = Get(player);

        if (component.Cubes is null || !component.Cubes.TryGetValue(cube, out var state))
        {
            return;
        }

        var session = world.Get<ServerSessionComponent>(player);
        var actor = world.Get<ActorComponent>(player);

        if (component.Taking != 0)
        {
            if (component.Taking != cube)
            {
                session.Send(new VaccumClickStartResponse { Actor = actor, Cube = cube, Result = Busy });
            }

            return;
        }

        component.Taking = cube;
        component.Ticket++;

        world.Set(player, component);

        session.Send(new VaccumClickStartResponse { Actor = actor, Cube = cube, Result = 0 });

        _ = CompleteAfterAsync(player, state, component.Ticket);
    }

    /// <summary>
    /// The person lets the cube go.
    /// </summary>
    public void ClickCancel(Entity player, int cube)
    {
        var component = Get(player);

        if (component.Taking == 0 || component.Taking != cube)
        {
            return;
        }

        component.Taking = 0;
        component.Ticket++;

        world.Set(player, component);

        world.Get<ServerSessionComponent>(player)
            .Send(new VaccumClickCancelResponse { Actor = world.Get<ActorComponent>(player), Cube = cube, Result = Released });
    }

    // Waits on the world loop, so what follows can use the world. A take that was cancelled or replaced finds a different ticket.
    private async Task CompleteAfterAsync(Entity player, VaccumCubeState state, int ticket)
    {
        try
        {
            await Task.Delay(state.Duration);

            if (!world.IsAlive(player))
            {
                return;
            }

            var component = Get(player);

            if (component.Ticket != ticket || component.Taking != state.Cube)
            {
                return;
            }

            var reward = cubeRewards.Roll(state.Value);
            var given = reward.IsEmpty || await rewards.TryGrantAsync(player, reward, RewardSource.Vaccum, CancellationToken.None);

            if (!world.IsAlive(player))
            {
                return;
            }

            component = Get(player);
            component.Taking = 0;
            component.Ticket++;
            component.Cubes?.Remove(state.Cube);

            world.Set(player, component);

            var session = world.Get<ServerSessionComponent>(player);

            session.Send(new VaccumClickCompleteResponse
            {
                Actor = world.Get<ActorComponent>(player),
                Cube = state.Cube,
                Remain = 0,
                Items = given ? reward.Items.Select(e => new RewardItemValue(e.Item, e.Count)).ToArray() : [],
                Result = given ? 0 : CannotTake
            });

            session.Send(new VaccumOutResponse { Cube = state.Cube, Flag = 0 });
        }

        catch (Exception e) when (e is not OperationCanceledException)
        {
            LogTakeFailed(e);
        }
    }

    private VaccumComponent Get(Entity player) =>
        world.IsAlive(player) && world.TryGet<VaccumComponent>(player, out var component) ? component : default;

    [LoggerMessage(Level = LogLevel.Error, Message = "Taking a cube failed.")]
    private partial void LogTakeFailed(Exception exception);
}
