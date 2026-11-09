using System.Numerics;
using Arch.Core;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Movement.Responses;
using OpenWorker.Lua;
using OpenWorker.Server.Common;
using OpenWorker.Wire;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// In a maze everybody sees everybody: the players of one instance, the members of a party, hear what each of them does,
/// and a player alone hears only themselves. A maze holds a party of four at most, so there is no grid of sight. A move
/// of a member waits for the end of the pass of the world loop and a later move in the same pass takes its place, as in a
/// district; a player alone gets the move at once.
/// </summary>
public sealed class MazeAudience(World world) : IPlayerAudience, IEndOfPass
{
    private readonly Dictionary<Entity, MovementMoveBtResponse> _moves = [];
    private readonly List<Entity> _moving = [];
    private bool _waiting;

    /// <summary>
    /// The maze of the player when others share it, otherwise nothing.
    /// </summary>
    private LuaMaze? Shared(Entity player)
    {
        return world.IsAlive(player) && world.TryGet(player, out LuaMaze? maze) && maze is { MemberCount: > 1 } ? maze : null;
    }

    public void Chat(Entity player, IResponseHotspotMessage message)
    {
        if (Shared(player) is { } maze)
        {
            SendMove(player);
            maze.Creatures.Broadcast(message);

            return;
        }

        world.Get<ServerSessionComponent>(player).Send(message);
    }

    public void Nearby<T>(Entity player, in T message) where T : struct, IResponseHotspotMessage, IWireWritable
    {
        using var frame = Frame.Encode(in message);

        if (frame is null)
        {
            return;
        }

        if (Shared(player) is { } maze)
        {
            SendMove(player);
            maze.Creatures.Broadcast(frame, Entity.Null);

            return;
        }

        world.Get<ServerSessionComponent>(player).Send(frame);
    }

    public void NearbyToOthers<T>(Entity player, in T message) where T : struct, IResponseHotspotMessage, IWireWritable
    {
        if (Shared(player) is not { } maze)
        {
            return;
        }

        SendMove(player);

        using var frame = Frame.Encode(in message);

        maze.Creatures.Broadcast(frame, player);
    }

    public void NearbyToOthers(Entity player, IResponseHotspotMessage message)
    {
        if (Shared(player) is not { } maze)
        {
            return;
        }

        SendMove(player);
        maze.Creatures.BroadcastToOthers(player, message);
    }

    /// <summary>
    /// A player alone gets the move at once. In a party it goes to every member at the end of the pass, the last one only.
    /// </summary>
    public void Move(Entity player, in MovementMoveBtResponse message)
    {
        if (Shared(player) is null || SynchronizationContext.Current is not WorldLoop { IsCurrent: true } loop)
        {
            Nearby(player, in message);

            return;
        }

        if (!_moves.ContainsKey(player))
        {
            _moving.Add(player);
        }

        _moves[player] = message;

        if (!_waiting)
        {
            _waiting = true;
            loop.AtEndOfPass(this);
        }
    }

    public void Relocate(Entity player, Vector3 position)
    {
    }

    void IEndOfPass.EndOfPass()
    {
        _waiting = false;

        foreach (var player in _moving)
        {
            SendMove(player);
        }

        _moving.Clear();
    }

    /// <summary>
    /// The move of the player that waits, before anything else the player does reaches the others.
    /// </summary>
    private void SendMove(Entity player)
    {
        if (!_moves.Remove(player, out var move))
        {
            return;
        }

        if (Shared(player) is { } maze)
        {
            using var frame = Frame.Encode(in move);

            maze.Creatures.Broadcast(frame, Entity.Null);
        }

        else if (world.IsAlive(player) && world.Has<ServerSessionComponent>(player))
        {
            world.Get<ServerSessionComponent>(player).Send(move);
        }
    }
}
