using Arch.Core;
using OpenWorker.Commands;
using OpenWorker.Domain.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Gameplay;
using OpenWorker.Hotspot.Modules.Chat.Enums;
using OpenWorker.Hotspot.Modules.Chat.Requests;
using OpenWorker.Hotspot.Modules.Chat.Responses;

namespace OpenWorker.MazeServer.Services;

[HotspotHandler(HotspotHandlerType.District)]
public sealed class ChatService(World world, CommandManager commands) : IHotspotHandler<ChatStuffRequest>
{
    public async ValueTask OnHandleAsync(ServiceHandleContext context, ChatStuffRequest request)
    {
        if (!await commands.TryExecute(context.Player, request.Message).ConfigureAwait(false))
        {
            return;
        }

        var session = world.Get<ServerSessionComponent>(context.Player);
        var actor = world.Get<ActorComponent>(context.Player);

        session.Send(new ChatNormalResponse
        {
            Actor = actor,
            Appearance = ChatMessageAppearance.System,
            Message = $"[CMD EXEC] {request.Message}"
        });
    }
}
