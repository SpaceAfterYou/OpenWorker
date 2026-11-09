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
/// the orbs of friends, and calling a helper into the maze (the maze server gives the place to call it to; a district
/// refuses).
/// </summary>
public sealed class HelperService(HelperManager manager, HelperGear gear, HelperSupports supports, IHelperField? field = null) :
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
    public async ValueTask OnHandleAsync(ServiceHandleContext context, HelperSummonRequest request)
    {
        if (field is null)
        {
            manager.Summon(context.Player, request.Summon != 0);
        }
        else if (request.Summon != 0)
        {
            await field.SummonAsync(context.Player, request.Helper, context.CancellationToken);
        }
        else
        {
            field.Release(context.Player, request.Helper);
        }
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
        // Only called helpers come back, and only in a maze. The answer is nothing but the new places of the helpers.
        field?.Recall(context.Player);

        return ValueTask.CompletedTask;
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, HelperAutoSummonRequest request)
    {
        await manager.SetAutoSummonAsync(context.Player, request.Person, request.Flag, context.CancellationToken);
    }
}
