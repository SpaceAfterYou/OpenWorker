using OpenWorker.Channel;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Gameplay;
using OpenWorker.Hotspot.Modules.Maze.Requests;
using OpenWorker.Hotspot.Modules.World.Requests;

namespace OpenWorker.DistrictServer.Services;

public sealed class WorldService(WorldManager manager, PartyMazeEntry party) :
    IHotspotHandler<MazeCreateRequest>,
    IHotspotHandler<WorldEnterRequest>,
    IHotspotHandler<MazeEnterPartyAnswerRequest>
{
    public async ValueTask OnHandleAsync(ServiceHandleContext context, MazeCreateRequest request)
    {
        // A member of a party enters the maze with the party.
        if (await party.TryEnterAsync(context.Player, request.Location, 0, MessageOpcode.From(GroupOpcode.World, WorldOpcode.MazeCreateReq)))
        {
            return;
        }

        await manager
            .TryEnterMaze(context.Player, request.Location);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, WorldEnterRequest request)
    {
        if (await party.TryEnterAsync(context.Player, request.Location, request.Jump, MessageOpcode.From(GroupOpcode.World, WorldOpcode.EnterReq)))
        {
            return;
        }

        await manager
            .EnterAsync(context.Player, request.Location, request.Jump);
    }

    /// <summary>
    /// A member answers the call of the party to enter a maze, or the master calls it off.
    /// </summary>
    public async ValueTask OnHandleAsync(ServiceHandleContext context, MazeEnterPartyAnswerRequest request)
    {
        await party.AnswerAsync(context.Player, request);
    }
}
