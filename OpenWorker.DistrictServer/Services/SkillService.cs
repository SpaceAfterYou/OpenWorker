using OpenWorker.Channel;
using OpenWorker.Gameplay.Messages.Response.Person;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Skill.Requests;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Modules.Skill;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Modules.System.Responses;

namespace OpenWorker.DistrictServer.Services;

/// <summary>
/// What the district answers of a fight; the skill window is <c>SkillWindowService</c> of both servers.
/// </summary>
public sealed class SkillService(World world) :
    IHotspotHandler<SkillAkashicRecordRequest>,
    IHotspotHandler<SkillChainRequest>,
    IHotspotHandler<SkillChargingStartRequest>,
    IHotspotHandler<SkillActiveSkillRequest>,
    IHotspotHandler<SkillKeypressRequest>,
    IHotspotHandler<SkillPassiveEndRequest>,
    IHotspotHandler<SkillPassiveRequest>,
    IHotspotHandler<SkillPreTargetListRequest>,
    IHotspotHandler<SkillProjectileRequest>,
    IHotspotHandler<SkillResetTagetRequest>,
    IHotspotHandler<SkillSubInputRequest>,
    IHotspotHandler<SkillSyncPositionRequest>,
    IHotspotHandler<SkillTargetChangeRequest>,
    IHotspotHandler<SkillTrapPosUpdateRequest>,
    IHotspotHandler<SkillWarpPositionRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillActiveSkillRequest request)
    {
        var session = world.Get<ServerSessionComponent>(context.Player);

        session.Send(new SkillActiveResponse());
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillAkashicRecordRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillChainRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillChargingStartRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillKeypressRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillPassiveEndRequest request)
    {
        var place = request.Skill.Position;

        if (Place.IsValid(place.Position, place.Angle))
        {
            world.Set(context.Player, world.Get<WorldComponent>(context.Player) with { Position = place.Position, Rotation = place.Angle });
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillPassiveRequest request)
    {
        world.Get<ServerSessionComponent>(context.Player).Send(new SkillPassiveResponse());

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillPreTargetListRequest request) =>
        ValueTask.CompletedTask;

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillProjectileRequest request) =>
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

    public ValueTask OnHandleAsync(ServiceHandleContext context, SkillWarpPositionRequest request) =>
        ValueTask.CompletedTask;
}
