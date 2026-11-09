using System.Numerics;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.System.Responses;

namespace OpenWorker.Gameplay.Modules.Npcs;

/// <summary>
/// The NPC a player asks a service of: its actor, its row of <c>tb_NPC</c>, the group of its credit and the id of the
/// function asked for (the shop group of a shop, 0 when the function has none).
/// </summary>
public readonly record struct ServiceNpc(int Actor, int Table, int Group, int Function);

/// <summary>
/// Why an NPC does not serve a request.
/// </summary>
public enum NpcRefusal : byte
{
    None = 0,

    /// <summary>
    /// No NPC of the city has that actor.
    /// </summary>
    Missing = 1,

    /// <summary>
    /// The player stands too far from it.
    /// </summary>
    Far = 2,

    /// <summary>
    /// The NPC does not offer the service, or offers another one of its kind (another recipe group, another kind of
    /// exchange).
    /// </summary>
    NoFunction = 3
}

/// <summary>
/// Where the NPCs of the place of a player stand: the server of a city knows the NPCs its map places, a maze knows none.
/// </summary>
public interface INpcPlaces
{
    /// <summary>
    /// The NPC with the actor identifier, as the player's city places it: its row of <c>tb_NPC</c> and where it stands.
    /// </summary>
    bool TryFind(Entity player, int actor, out int table, out Vector3 position);
}

/// <summary>
/// Used where the server places no NPCs itself: no service is served there.
/// </summary>
public sealed class NoNpcPlaces : INpcPlaces
{
    public bool TryFind(Entity player, int actor, out int table, out Vector3 position)
    {
        table = 0;
        position = default;
        return false;
    }
}

/// <summary>
/// The one check of the NPC behind a service of a city (a shop, a repair, an upgrade, production, an exchange): the NPC the
/// request names stands in the player's city, the player stands near it, and its row offers the service.
/// </summary>
public sealed class ServiceNpcs(World world, NpcCatalog catalog, INpcPlaces places)
{
    /// <summary>
    /// How far, on each axis, a player may stand from the NPC it is served by. The client opens the dialog of an NPC
    /// closer than this; the rest is room for a move the server has not heard yet.
    /// </summary>
    public const float Reach = 1000f;

    public NpcCatalog Catalog => catalog;

    /// <summary>
    /// Finds the NPC of a request. <paramref name="function"/> <see cref="NpcFunction.None"/> asks for no function;
    /// <paramref name="functionId"/> other than 0 asks for that id of the function as well (a recipe group), and
    /// <paramref name="also"/> for a second function the NPC must offer too (the kind of NPC an exchange is made at).
    /// </summary>
    public NpcRefusal Find(Entity player, ActorValue actor, NpcFunction function, out ServiceNpc npc, int functionId = 0,
        NpcFunction also = NpcFunction.None)
    {
        npc = default;

        // The client names an NPC by the actor the city gave it, with the kind of actor in its high bits or without it.
        if (actor.Type is not (ActorType.Npc or ActorType.User) || !world.IsAlive(player) ||
            !places.TryFind(player, actor.Identifier, out var table, out var position))
        {
            return NpcRefusal.Missing;
        }

        if (!world.TryGet<WorldComponent>(player, out var place) || !IsNear(place.Position, position))
        {
            return NpcRefusal.Far;
        }

        var id = 0;

        if (function != NpcFunction.None)
        {
            if (catalog.FunctionOf(table, function) is not { } offered || (functionId != 0 && offered != functionId))
            {
                return NpcRefusal.NoFunction;
            }

            id = offered;
        }

        if (also != NpcFunction.None && catalog.FunctionOf(table, also) is null)
        {
            return NpcRefusal.NoFunction;
        }

        npc = new ServiceNpc(actor.Identifier, table, catalog.GroupOf(table), id);

        return NpcRefusal.None;
    }

    /// <summary>
    /// Finds the NPC of a request, or answers the request with the error of the service and gives <c>false</c>, so the
    /// dialog of the client stops waiting and nothing changes.
    /// </summary>
    public bool TryServe(Entity player, int actor, NpcFunction function, MessageOpcode request, ushort refusal, out ServiceNpc npc,
        int functionId = 0, NpcFunction also = NpcFunction.None)
    {
        if (Find(player, actor, function, out npc, functionId, also) == NpcRefusal.None)
        {
            return true;
        }

        if (world.IsAlive(player) && world.TryGet<ServerSessionComponent>(player, out var session))
        {
            session.Send(new ErrorMessageResponse(request, refusal));
        }

        return false;
    }

    private static bool IsNear(Vector3 player, Vector3 npc)
    {
        return MathF.Abs(player.X - npc.X) <= Reach && MathF.Abs(player.Y - npc.Y) <= Reach && MathF.Abs(player.Z - npc.Z) <= Reach;
    }
}
