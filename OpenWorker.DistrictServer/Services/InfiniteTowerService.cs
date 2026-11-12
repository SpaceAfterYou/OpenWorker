using Arch.Core;
using OpenWorker.Channel;
using OpenWorker.Channel.Community;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Modules.Tower;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.InfiniteTower.Requests;
using OpenWorker.Hotspot.Modules.System.Responses;

namespace OpenWorker.DistrictServer.Services;

/// <summary>
/// The window of the tower in a district: the person climbs a chapter, alone, from its first stage (<see cref="InfiniteTower"/>).
/// A refusal is the error answer the client shows; a chapter that may be climbed sends the person into the maze of its first
/// stage the way a portal of a maze does.
/// </summary>
public sealed class InfiniteTowerService(World world, InfiniteTower tower, WorldManager manager, IPartyRoster roster) :
    IHotspotHandler<InfiniteTowerEnterChapterRequest>
{
    public async ValueTask OnHandleAsync(ServiceHandleContext context, InfiniteTowerEnterChapterRequest request)
    {
        var person = world.Get<ActorComponent>(context.Player).Identifier;
        var party = await roster.PartyAsync(person);

        if (!world.IsAlive(context.Player))
        {
            return;
        }

        var session = world.Get<ServerSessionComponent>(context.Player);

        if (tower.CheckEnter(context.Player, request.Chapter, party is { Members.Count: > 1 }, out var maze) is { } refusal)
        {
            session.Send(new ErrorMessageResponse(refusal.Request, refusal.Code));

            return;
        }

        // The map of the first stage has no way in: the data is not what the tower needs.
        if (!await manager.TryEnterMaze(context.Player, maze) && world.IsAlive(context.Player))
        {
            session.Send(new ErrorMessageResponse(InfiniteTower.EnterChapterRequest, InfiniteTower.InvalidData));
        }
    }
}
