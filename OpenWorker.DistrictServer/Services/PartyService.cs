using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Party.Requests;

namespace OpenWorker.DistrictServer.Services;

public sealed class PartyService : IHotspotHandler<PartyInviteRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, PartyInviteRequest request)
    {
        // TODO: var session = world.Get<ServerSessionComponent>(context.Player);

        return ValueTask.CompletedTask;
    }
}
