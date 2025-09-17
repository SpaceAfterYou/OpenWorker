using Arch.Core;
using OpenWorker.DistrictServer.Gameplay;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Channels.Requests;
using OpenWorker.Hotspot.Modules.Channels.Responses;
using OpenWorker.Hotspot.Modules.Gestures.Request;

namespace OpenWorker.DistrictServer.Services;

public sealed class SkillService(World world) :
    IHotspotHandler<SkillPassiveEndRequest>,
    IHotspotHandler<SkillDeckBonusRequest>,
    IHotspotHandler<SkillActiveSkillRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillPassiveEndRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);
        
        session.Send(new SkillPassiveResponse(0));

        return ValueTask.CompletedTask;
    }
    
    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillDeckBonusRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);
        
        session.Send(new SkillDeckBonusResponse(request.Deck));

        return ValueTask.CompletedTask;
    }
    
    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillActiveSkillRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);
        
        session.Send(new SkillActiveSkillResponse(0, 0, false));
        
        return ValueTask.CompletedTask;
    }
}

public sealed class GestureService(GestureGameplay gameplay) :
    IHotspotHandler<GestureShowRequest>,
    IHotspotHandler<GestureSlotUpdateRequest>
{
    public async ValueTask OnHandleAsync(ServiceHandleContext context, GestureShowRequest request)
    {
        await gameplay
            .ShowAsync(context, request)
            .ConfigureAwait(false);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, GestureSlotUpdateRequest request)
    {
        await gameplay
            .UpdateAsync(context, request)
            .ConfigureAwait(false);
    }
}