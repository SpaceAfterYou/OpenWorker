using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Types;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Gameplay.Modules.Npcs;
using OpenWorker.Gameplay.Modules.Npcs.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Movement.Requests;
using OpenWorker.Hotspot.Modules.Movement.Responses;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Server.Common;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// What a ride needs of the map the rider stands in: where a box of the map is, and a rider that nothing may hurt.
/// </summary>
public interface ITransportMap
{
    /// <summary>
    /// The middle of the box of the map and the way it faces, when the map has such a box to stand at.
    /// </summary>
    bool TryFindStop(Entity player, int box, out Vector3 position, out float rotation);

    /// <summary>
    /// Nothing hurts the player for the given time.
    /// </summary>
    void Protect(Entity player, TimeSpan length);
}

/// <summary>
/// The rides of the NPCs of transports. The player asks one of them for a ride (05/16: its actor, the row of the ride, the
/// NPC); the NPC must stand near the player and offer the ride, and the player pays the fare less its credit with the group
/// of the NPC and its boosters. Then everybody who sees the player is told it rides (05/16), the client plays the path of
/// the ride and keeps the input of the player locked until the server ends the ride, which the client cannot do: when the
/// seconds of the ride have passed on the game clock, the player is put in the middle of the box the ride ends at and
/// everybody is told (05/17). While it rides the player moves by no request of its own and nothing hurts it.
/// </summary>
/// <remarks>
/// The rides are in District 6, the map of the four towns' stations, which is a maze here. The server does not follow the
/// path: the player stays where it paid until the ride is over, and then stands at the end.
/// </remarks>
public sealed partial class TransportService(
    World world,
    ServiceNpcs npcs,
    TransportTables tables,
    ITransportMap map,
    InventoryService inventory,
    NpcCreditManager credit,
    IPlayerAudience audience,
    SimulationClock clock,
    HandlerExecutor errors,
    ILogger<TransportService> logger) :
    IHotspotHandler<MovementTransportTakeRequest>
{
    /// <summary>
    /// How long the player waits at the start before the path begins to play, on top of the seconds of the ride.
    /// </summary>
    public static TimeSpan Departure { get; } = TimeSpan.FromSeconds(1);

    public async ValueTask OnHandleAsync(ServiceHandleContext context, MovementTransportTakeRequest request)
    {
        var player = context.Player;

        // A player asks for itself, and only after it has entered the game through this connection.
        if (!world.IsAlive(player) || world.Get<ClaimsComponent>(player).Account == 0 ||
            world.Get<ActorComponent>(player).Identifier != request.Actor.Identifier)
        {
            errors.Charge(player, request.Opcode, HandlerErrorReason.Violation);
            return;
        }

        // The maze does not load the credit of a person when it enters; the first ride does.
        if (!world.TryGet(player, out NpcCreditComponent book) || book.Book is null)
        {
            await credit.EnterAsync(player, world.Get<ActorComponent>(player).Identifier, context.CancellationToken);

            if (!world.IsAlive(player))
            {
                return;
            }
        }

        // Nothing below waits: the checks and the payment are one step of the world loop.
        if (world.Has<TransportRideComponent>(player))
        {
            Refuse(player, request, ServiceNpcCodes.Transport);
            return;
        }

        if (!npcs.TryServe(player, request.Npc, NpcFunction.Transport, request.Opcode, ServiceNpcCodes.Transport, out var npc))
        {
            return;
        }

        if (!tables.TryGetRide(npc.Function, request.Row, out var ride) ||
            !map.TryFindStop(player, ride.PositionBox, out var destination, out var rotation))
        {
            Refuse(player, request, ServiceNpcCodes.TransportRide);
            return;
        }

        var fare = credit.Discount(player, npc, CreditBenefit.TransportMoney, ride.NeedGold);

        if (fare > 0 && !inventory.TryAddGold(player, -fare))
        {
            Refuse(player, request, ServiceNpcCodes.TransportGold);
            return;
        }

        // A ride that names no path or takes no time carries the player at once.
        var length = ride.PathFile == "0" || ride.RideSeconds <= 0 ? TimeSpan.Zero : Departure + TimeSpan.FromSeconds(ride.RideSeconds);

        world.Add(player, new TransportRideComponent { Row = ride.Id, Destination = destination, Rotation = rotation });

        map.Protect(player, length);

        audience.Nearby(player, new MovementTransportTakeResponse { Actor = request.Actor, Row = request.Row, Npc = request.Npc });

        clock.Schedule(length, () => End(player));

        credit.Raise(player, npc, CreditRaise.Transport);

        LogRide(logger, request.Actor.Identifier, ride.Id, fare, length.TotalSeconds);
    }

    /// <summary>
    /// The ride is over: the player stands at the end of it and everybody who sees it is told.
    /// </summary>
    private void End(Entity player)
    {
        // The player may have left meanwhile; the ride goes with it.
        if (!world.IsAlive(player) || !world.Has<TransportRideComponent>(player))
        {
            return;
        }

        var ride = world.Get<TransportRideComponent>(player);

        world.Remove<TransportRideComponent>(player);

        if (world.TryGet(player, out WorldComponent place))
        {
            world.Set(player, place with { Position = ride.Destination, Rotation = ride.Rotation });
        }

        var actor = world.Get<ActorComponent>(player);

        // Told first, to everybody who has watched the ride; then the player moves in their sight to where it ends.
        audience.Nearby(player, new MovementTransportOffResponse { Actor = new ActorValue(actor.Identifier, actor.Type) });
        audience.Relocate(player, ride.Destination);

        LogEnd(logger, actor.Identifier, ride.Row);
    }

    private void Refuse(Entity player, MovementTransportTakeRequest request, ushort code)
    {
        world.Get<ServerSessionComponent>(player).Send(new ErrorMessageResponse(request.Opcode, code));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Actor {Actor} rides row {Row} for {Fare} gold, {Seconds} seconds.")]
    private static partial void LogRide(ILogger logger, int actor, short row, long fare, double seconds);

    [LoggerMessage(Level = LogLevel.Information, Message = "Actor {Actor} has arrived from row {Row}.")]
    private static partial void LogEnd(ILogger logger, int actor, short row);
}
