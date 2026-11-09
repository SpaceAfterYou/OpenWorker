using OpenWorker.Gameplay.Modules.Helpers;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Modules.Helpers.Requests;

namespace OpenWorker.Server.Common.Services;

/// <summary>
/// The window of the helpers, in a district and in a maze: the list, its order, the automatic call, and the refusals of
/// what is not done yet (calling a helper into a maze, items, the orbs of friends).
/// </summary>
public sealed class HelperService(HelperManager manager) :
    IHotspotHandler<HelperSummonRequest>,
    IHotspotHandler<HelperSupportInfoRequest>,
    IHotspotHandler<HelperSupportRegisterRequest>,
    IHotspotHandler<HelperSupportRewardRequest>,
    IHotspotHandler<HelperSupportListRequest>,
    IHotspotHandler<HelperSupportEquipRequest>,
    IHotspotHandler<HelperEquipRequest>,
    IHotspotHandler<HelperChangeOrderRequest>,
    IHotspotHandler<HelperRecallRequest>,
    IHotspotHandler<HelperAutoSummonRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, HelperSummonRequest request)
    {
        manager.Summon(context.Player, request.Summon != 0);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, HelperSupportInfoRequest request)
    {
        manager.SendSupportInfo(context.Player);

        return ValueTask.CompletedTask;
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, HelperSupportRegisterRequest request)
    {
        await manager.RegisterSupportAsync(context.Player, request.SupportType, context.CancellationToken);
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, HelperSupportRewardRequest request)
    {
        manager.RefuseSupportReward(context.Player);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, HelperSupportListRequest request)
    {
        manager.SendSupportList(context.Player);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, HelperSupportEquipRequest request)
    {
        manager.RefuseEquip(context.Player, HelperOpcode.SupportEquip);

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, HelperEquipRequest request)
    {
        manager.RefuseEquip(context.Player, HelperOpcode.Equip);

        return ValueTask.CompletedTask;
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, HelperChangeOrderRequest request)
    {
        await manager.ChangeOrderAsync(context.Player, request.First, request.Second, context.CancellationToken);
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, HelperRecallRequest request)
    {
        // Only called helpers come back, and none is called yet. The original answers a recall with nothing but the
        // new positions of the helpers.
        return ValueTask.CompletedTask;
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, HelperAutoSummonRequest request)
    {
        await manager.SetAutoSummonAsync(context.Player, request.Person, request.Flag, context.CancellationToken);
    }
}
