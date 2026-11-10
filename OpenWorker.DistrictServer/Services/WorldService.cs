using Arch.Core;
using OpenWorker.Channel;
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

public sealed class WorldService(World world, WorldManager manager, PartyMazeEntry party, MazeAdmission admission) :
    IHotspotHandler<MazeCreateRequest>,
    IHotspotHandler<WorldEnterRequest>,
    IHotspotHandler<MazeEnterPartyAnswerRequest>
{
    public async ValueTask OnHandleAsync(ServiceHandleContext context, MazeCreateRequest request)
    {
        var opcode = MessageOpcode.From(GroupOpcode.World, WorldOpcode.MazeCreateReq);

        // A member of a party enters the maze with the party.
        if (await party.TryEnterAsync(context.Player, request.Location, 0, opcode, request.Portal) ||
            Refused(context, request.Location, request.Portal, opcode))
        {
            return;
        }

        await manager
            .TryEnterMaze(context.Player, request.Location);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, WorldEnterRequest request)
    {
        var opcode = MessageOpcode.From(GroupOpcode.World, WorldOpcode.EnterReq);

        if (await party.TryEnterAsync(context.Player, request.Location, request.Jump, opcode, request.Portal) ||
            Refused(context, request.Location, request.Portal, opcode))
        {
            return;
        }

        await manager
            .EnterAsync(context.Player, request.Location, request.Jump);
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

        world.Get<ServerSessionComponent>(context.Player).Send(new ErrorMessageResponse(request, code, item > 0 ? (uint)item : null));

        return true;
    }

    /// <summary>
    /// A member answers the call of the party to enter a maze, or the master calls it off.
    /// </summary>
    public async ValueTask OnHandleAsync(ServiceHandleContext context, MazeEnterPartyAnswerRequest request)
    {
        await party.AnswerAsync(context.Player, request);
    }
}
