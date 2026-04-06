using Arch.Core;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Gameplay;
using OpenWorker.Hotspot.Modules.Party.Requests;

namespace OpenWorker.DistrictServer.Services;

public sealed class PartyService(World world) : IHotspotHandler<PartyInviteRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyInviteRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.GetPlayerEntity());
        
        return ValueTask.CompletedTask;
    }
}