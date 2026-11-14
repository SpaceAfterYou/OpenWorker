using Arch.Core;
using OpenWorker.Channel;
using OpenWorker.Channel.Community;
using OpenWorker.Domain.Components;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Gameplay;
using OpenWorker.Gameplay.Modules.Maze;
using OpenWorker.Hotspot.Modules.Maze.Requests;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Hotspot.Modules.World.Requests;

namespace OpenWorker.DistrictServer.Services;

public sealed class WorldService(
    World world,
    WorldManager manager,
    PartyMazeEntry party,
    MazeAdmission admission,
    IPartyRoster roster,
    IPartyMazeRoutes routes,
    ServerChanges changes,
    WorldMazes? worlds = null) :
    IHotspotHandler<MazeCreateRequest>,
    IHotspotHandler<WorldEnterRequest>,
    IHotspotHandler<MazeEnterPartyAnswerRequest>
{
    /// <summary>
    /// The player picks a maze on the board of the portal it stands at, or joins the maze its party plays (the client asks
    /// from anywhere when a member has gone in). Any other maze is refused before the party is looked at.
    /// </summary>
    public async ValueTask OnHandleAsync(ServiceHandleContext context, MazeCreateRequest request)
    {
        var opcode = MessageOpcode.From(GroupOpcode.World, WorldOpcode.MazeCreateReq);

        // A player already told where to go is not sent anywhere again.
        if (changes.IsLeaving(context.Player))
        {
            return;
        }

        if (admission.CheckBoard(context.Player, request.Location, request.Portal) is { } astray &&
            !await PartyPlaysAsync(context.Player, request.Location))
        {
            Refuse(context.Player, opcode, astray, 0);

            return;
        }

        // A member of a party enters the maze with the party.
        if (!world.IsAlive(context.Player) ||
            await party.TryEnterAsync(context.Player, request.Location, 0, opcode, request.Portal) ||
            Refused(context, request.Location, request.Portal, opcode))
        {
            return;
        }

        await manager
            .TryEnterMaze(context.Player, request.Location);
    }

    /// <summary>
    /// The player takes a portal of the district: it leads where the map says, from the start point the map says
    /// (<see cref="MazeAdmission.CheckWay"/>), and then asks what the location asks.
    /// </summary>
    public async ValueTask OnHandleAsync(ServiceHandleContext context, WorldEnterRequest request)
    {
        var opcode = MessageOpcode.From(GroupOpcode.World, WorldOpcode.EnterReq);

        if (!world.IsAlive(context.Player) || changes.IsLeaving(context.Player))
        {
            return;
        }

        if (admission.CheckWay(context.Player, request.Location, request.Portal, request.Jump, out var start) is { } astray)
        {
            Refuse(context.Player, opcode, astray, 0);

            return;
        }

        // The open maze of the world takes everybody of the channel, a party or not.
        var open = worlds?.IsWorld(request.Location) == true;

        if ((!open && await party.TryEnterAsync(context.Player, request.Location, start, opcode, request.Portal)) ||
            Refused(context, request.Location, request.Portal, opcode))
        {
            return;
        }

        await manager
            .EnterAsync(context.Player, request.Location, start);
    }

    /// <summary>
    /// Whether the party of the player, of more than one, plays the maze now.
    /// </summary>
    private async ValueTask<bool> PartyPlaysAsync(Entity player, short location)
    {
        if (!world.IsAlive(player) || !world.TryGet<ActorComponent>(player, out var actor))
        {
            return false;
        }

        var found = await roster.PartyAsync(actor.Identifier);

        if (found is null || found.Members.Count < 2 || !world.IsAlive(player))
        {
            return false;
        }

        var running = await routes.FindAsync(found.Id);

        return running is not null && running.Location == location;
    }

    /// <summary>
    /// A player alone who may not enter the maze it takes the way into is told why (<see cref="MazeAdmission"/>).
    /// </summary>
    private bool Refused(ServiceHandleContext context, short location, int portal, MessageOpcode request)
    {
        if (!world.IsAlive(context.Player) || admission.Check(context.Player, location, portal, out var item) is not { } code)
        {
            return false;
        }

        Refuse(context.Player, request, code, item);

        return true;
    }

    private void Refuse(Entity player, MessageOpcode request, ushort code, int item)
    {
        if (world.IsAlive(player))
        {
            world.Get<ServerSessionComponent>(player).Send(new ErrorMessageResponse(request, code, item > 0 ? (uint)item : null));
        }
    }

    /// <summary>
    /// A member answers the call of the party to enter a maze, or the master calls it off.
    /// </summary>
    public async ValueTask OnHandleAsync(ServiceHandleContext context, MazeEnterPartyAnswerRequest request)
    {
        await party.AnswerAsync(context.Player, request);
    }
}
