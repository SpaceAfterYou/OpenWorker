using OpenWorker.Channel.Community;
using OpenWorker.Gameplay.Modules.Helpers;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Modules.Helpers.Requests;

namespace OpenWorker.Server.Common.Services;

/// <summary>
/// The window of the helpers, in a district and in a maze: the list, its order, the automatic call, the items helpers wear,
/// the orbs of friends, and the refusal of what is not done yet (calling a helper into a maze).
/// </summary>
public sealed class HelperService(HelperManager manager, HelperGear gear, HelperSupports supports) :
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

    public async ValueTask OnHandleAsync(ServiceHandleContext context, HelperSupportInfoRequest request)
    {
        await supports.SendInfoAsync(context.Player);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, HelperSupportRegisterRequest request)
    {
        await supports.RegisterAsync(context.Player, request.SupportType);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, HelperSupportRewardRequest request)
    {
        await supports.RewardAsync(context.Player);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, HelperSupportListRequest request)
    {
        await supports.SendListAsync(context.Player);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, HelperSupportEquipRequest request)
    {
        await supports.EquipAsync(context.Player, request.Helper, request.Friend);
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, HelperEquipRequest request)
    {
        gear.Equip(context.Player, request);

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
