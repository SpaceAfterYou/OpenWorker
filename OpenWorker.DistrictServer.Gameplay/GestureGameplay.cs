using Arch.Core;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Gestures.Components;
using OpenWorker.Hotspot.Modules.Gestures.Request;
using OpenWorker.Hotspot.Modules.Gestures.Responses;
using OpenWorker.Persistence;

namespace OpenWorker.DistrictServer.Gameplay;

public sealed class GestureGameplay(World world, IDbContextFactory<PersistenceContext> factory)
{
    public ValueTask ShowAsync(ServiceHandleContext context, GestureShowRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);
        var actor = world.Get<ActorComponent>(context.Player);

        session.Send(new GestureShowResponse(request.Identifier, actor));

        var gesture = world.Get<GestureComponent>(context.Player);
        
        world.Set(context.Player, gesture with { Active = request.Identifier });
        
        return ValueTask.CompletedTask;
    }

    public async ValueTask UpdateAsync(ServiceHandleContext context, GestureSlotUpdateRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);
        var gesture = world.Get<GestureComponent>(context.Player);

        var length = Math.Min(gesture.Collection.Length, request.GestureList.Length);

        Array.Copy(request.GestureList, gesture.Collection, length);

        await using var persistence = await factory
            .CreateDbContextAsync(context.CancellationToken)
            .ConfigureAwait(false);

        var actor = world.Get<ActorComponent>(context.Player);
        
        var person = persistence.Persons.First(x => x.Id == actor.Identifier);

        person.GestureList = gesture.Collection.ToArray(); 
        
        persistence.Persons.Update(person);
        
        await persistence
            .SaveChangesAsync(context.CancellationToken)
            .ConfigureAwait(false);
        
        session.Send(new GestureSlotUpdateResponse(gesture.Collection));
    }
}
