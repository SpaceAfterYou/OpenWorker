using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Skill.Requests;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.DistrictServer.Services;

[HotspotHandler(HotspotHandlerType.District)]
public sealed class SkillService(World world, ReadOnlyCollection<SkillRow> skills) :
    IHotspotHandler<SkillDeckBonusRequest>,
    IHotspotHandler<SkillAddDeckSlotRequest>,
    IHotspotHandler<SkillAkashicRecordRequest>,
    IHotspotHandler<SkillChainRequest>,
    IHotspotHandler<SkillChargingStartRequest>,
    IHotspotHandler<SkillActiveSkillRequest>,
    IHotspotHandler<SkillDivergenceLearnRequest>,
    IHotspotHandler<SkillKeypressRequest>,
    IHotspotHandler<SkillLearnRequest>,
    IHotspotHandler<SkillPassiveEndRequest>,
    IHotspotHandler<SkillPassiveRequest>,
    IHotspotHandler<SkillPreTargetListRequest>,
    IHotspotHandler<SkillProjectileRequest>,
    IHotspotHandler<SkillResetRequest>,
    IHotspotHandler<SkillResetTagetRequest>,
    IHotspotHandler<SkillSubInputRequest>,
    IHotspotHandler<SkillSyncPositionRequest>,
    IHotspotHandler<SkillTargetChangeRequest>,
    IHotspotHandler<SkillTrapPosUpdateRequest>,
    IHotspotHandler<SkillUpdateDeckRequest>,
    IHotspotHandler<SkillWarpPositionRequest>
{
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

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillAddDeckSlotRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillAkashicRecordRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillChainRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillChargingStartRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillDeckBonusRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);

        session.Send(new SkillDeckBonusResponse
        {
            Deck = request.Deck,
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillDivergenceLearnRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillKeypressRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillLearnRequest request)
    {
        ref readonly var session = ref world.Get<ServerSessionComponent>(context.Player);

        var skill = skills.First(e => e.Id == request.Info.Skill);

        session.Send(new SkillLearnResponse
            {
                Info = request.Info,
                Type = 0,
                IsSuccessful = true
            });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillPassiveEndRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);

        session.Send(new SkillPassiveResponse
        {
            Error = 0,
        });

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillPassiveRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillPreTargetListRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillProjectileRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillResetRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillResetTagetRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillSubInputRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillSyncPositionRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillTargetChangeRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillTrapPosUpdateRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillUpdateDeckRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillWarpPositionRequest request) =>
        ValueTask.CompletedTask;
}
