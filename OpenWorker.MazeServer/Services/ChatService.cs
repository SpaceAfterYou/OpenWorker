using Arch.Core;
using OpenWorker.Channel;
using OpenWorker.Commands;
using OpenWorker.Domain.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Chat.Enums;
using OpenWorker.Hotspot.Modules.Chat.Requests;
using OpenWorker.Hotspot.Modules.Chat.Responses;
using Redis.OM.Searching;

namespace OpenWorker.MazeServer.Services;

[HotspotHandler(HotspotHandlerType.District)]
public sealed class ChatService(World world, StuffCommands commands) :
    IHotspotHandler<ChatStuffRequest>
{
    public async ValueTask OnHandleAsync(ServiceHandleContext context, ChatStuffRequest request)
    {
        if (!await commands.TryExecute(context.Player, request.Message).ConfigureAwait(false))
        {
            return;
        }

        var session = world.Get<ServerSessionComponent>(context.Player);
        var actor = world.Get<ActorComponent>(context.Player);

        session.Send(new ChatNormalResponse(actor, ChatMessageAppearance.System, $"[CMD EXEC] {request.Message}"));
    }
}