using Arch.Core;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Skill.Requests;
using OpenWorker.Hotspot.Modules.Skill.Responses;

namespace OpenWorker.DistrictServer.Services;

public sealed class SkillService(World world) :
    IHotspotHandler<SkillPassiveEndRequest>,
    IHotspotHandler<SkillDeckBonusRequest>,
    IHotspotHandler<SkillActiveSkillRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillPassiveEndRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);

        session.Send(new SkillPassiveResponse
        {
            Error = 0,
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillDeckBonusRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);

        session.Send(new SkillDeckBonusResponse
        {
            Deck = request.Deck,
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillActiveSkillRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);

        session.Send(new SkillActiveResponse
        {
            ErrorCode = 0,
            Divergence = 0,
            Swap = false,
        });

        return ValueTask.CompletedTask;
    }
}
