using Arch.Core;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.SocialItems;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.SocialItems.Requests;
using OpenWorker.Hotspot.Modules.System.Responses;

namespace OpenWorker.DistrictServer.Services;

/// <summary>
/// The requests of the social objects in a district: putting one down, using one and stopping. A refusal is the error answer of
/// 2D/01 with its code, whatever the request, as the original answers.
/// </summary>
public sealed class SocialItemService(World world, SocialItems objects) :
    IHotspotHandler<SocialItemStartRequest>,
    IHotspotHandler<SocialItemUseRequest>,
    IHotspotHandler<SocialItemStopRequest>
{
    public async ValueTask OnHandleAsync(ServiceHandleContext context, SocialItemStartRequest request)
    {
        Answer(context.Player, await objects.StartAsync(context.Player, request));
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SocialItemUseRequest request)
    {
        Answer(context.Player, objects.Use(context.Player, request));

        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, SocialItemStopRequest request)
    {
        Answer(context.Player, objects.Stop(context.Player));

        return ValueTask.CompletedTask;
    }

    private void Answer(Entity player, ushort code)
    {
        if (code != 0 && world.IsAlive(player) && world.TryGet<ServerSessionComponent>(player, out var session))
        {
            session.Send(new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.SocialItem, SocialItemOpcode.Start), code));
        }
    }
}
