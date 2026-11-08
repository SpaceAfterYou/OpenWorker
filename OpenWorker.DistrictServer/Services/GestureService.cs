using System.Collections.ObjectModel;
using Arch.Core;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Channel;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Modules.Gestures.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Gestures.Requests;
using OpenWorker.Hotspot.Modules.Gestures.Responses;
using OpenWorker.Persistence;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.DistrictServer.Services;

/// <summary>
/// Gestures: showing one to the others, and the six a player keeps ready.
/// </summary>
[HotspotHandler(HotspotHandlerType.District)]
public sealed class GestureService(
    World world,
    ServiceChannels channels,
    IDbContextFactory<PersistenceContext> factory,
    ReadOnlyCollection<GestureRow> table
) :
    IHotspotHandler<GestureShowRequest>,
    IHotspotHandler<GestureSlotUpdateRequest>
{
    private HashSet<int> Known { get; } = table.Select(e => (int)e.Id).ToHashSet();

    /// <summary>
    /// Everybody in the channel sees the gesture, the one who makes it too.
    /// </summary>
    public ValueTask OnHandleAsync(ServiceHandleContext context, GestureShowRequest request)
    {
        if (!Known.Contains(request.Identifier))
        {
            return ValueTask.CompletedTask;
        }

        var gesture = world.Get<GestureComponent>(context.Player);

        world.Set(context.Player, gesture with { Active = request.Identifier });

        channels.Get(context.Player).Broadcast(new GestureShowResponse
        {
            Gesture = request.Identifier,
            Actor = world.Get<ActorComponent>(context.Player)
        });

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// The slots are kept as sent when every gesture in them exists and none is in two slots. Either way the client is told
    /// what the slots are now: it only reads the list of slots, not an answer to the change.
    /// </summary>
    public async ValueTask OnHandleAsync(ServiceHandleContext context, GestureSlotUpdateRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);
        var gesture = world.Get<GestureComponent>(context.Player);

        var valid = request.GestureList.All(e => e == 0 || Known.Contains(e)) &&
                    request.GestureList.Where(e => e != 0).Distinct().Count() == request.GestureList.Count(e => e != 0);

        if (!valid)
        {
            session.Send(new GestureSlotLoadResponse { Gestures = gesture.Collection });
            return;
        }

        var slots = request.GestureList.ToArray();

        world.Set(context.Player, gesture with { Collection = slots });

        session.Send(new GestureSlotLoadResponse { Gestures = slots });

        await factory.SaveGesturesAsync(world.Get<ActorComponent>(context.Player).Identifier, slots);
    }
}
