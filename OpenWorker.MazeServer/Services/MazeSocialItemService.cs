using Arch.Core;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.SocialItems;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.SocialItems.Requests;
using OpenWorker.Hotspot.Modules.System.Responses;

namespace OpenWorker.MazeServer.Services;

/// <summary>
/// Social objects stand in districts only: a maze refuses every request of them, so the client stops waiting.
/// </summary>
public sealed class MazeSocialItemService(World world) :
    IHotspotHandler<SocialItemStartRequest>,
    IHotspotHandler<SocialItemUseRequest>,
    IHotspotHandler<SocialItemStopRequest>
{
    public ValueTask OnHandleAsync(ServiceHandleContext context, SocialItemStartRequest request) =>
        Refuse(context.Player, SocialItemCodes.NotDistrict);

    public ValueTask OnHandleAsync(ServiceHandleContext context, SocialItemUseRequest request) =>
        Refuse(context.Player, SocialItemCodes.NotDistrict);

    public ValueTask OnHandleAsync(ServiceHandleContext context, SocialItemStopRequest request) =>
        Refuse(context.Player, SocialItemCodes.StopNotDistrict);

    private ValueTask Refuse(Entity player, ushort code)
    {
        if (world.IsAlive(player) && world.TryGet<ServerSessionComponent>(player, out var session))
        {
            session.Send(new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.SocialItem, SocialItemOpcode.Start), code));
        }

        return ValueTask.CompletedTask;
    }
}
