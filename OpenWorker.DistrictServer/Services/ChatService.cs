using Arch.Core;
using Arch.Core.Extensions;
using OpenWorker.Channel;
using OpenWorker.Commands;
using OpenWorker.Domain.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Cache.Types;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Gameplay;
using OpenWorker.Hotspot.Modules.Chat.Enums;
using OpenWorker.Hotspot.Modules.Chat.Requests;
using OpenWorker.Hotspot.Modules.Chat.Responses;
using Redis.OM.Searching;

namespace OpenWorker.DistrictServer.Services;

[HotspotHandler(HotspotHandlerType.District)]
public sealed class ChatService(World world, CommandManager commands, ServiceChannels channels, IRedisCollection<ChannelChatCache> channelChatCache) :
    IHotspotHandler<ChatNormalRequest>,
    IHotspotHandler<ChatWhisperRequest>,
    IHotspotHandler<ChatTradeRequest>,
    IHotspotHandler<ChatMegaphoneRequest>,
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

        session.Send(new ChatNormalResponse
        {
            Actor = actor,
            Appearance = ChatMessageAppearance.System,
            Message = $"[CMD EXEC] {request.Message}"
        });
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ChatMegaphoneRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, ChatNormalRequest request)
    {
        var channel = channels.Get(context.Player);
        var actor = world.Get<ActorComponent>(context.Player);

        channel.ForEach(e =>
        {
            var session = e.Get<ServerSessionComponent>();
            session.Send(new ChatNormalResponse
            {
                Actor = actor,
                Appearance = ChatMessageAppearance.Normal,
                Message = request.Message
            });
        });

        var cache = new ChannelChatCache
        {
            Channel = channel.Identifier,
            Message = request.Message,
            Appearance = ChatMessageAppearance.Normal,
            Actor = actor.Identifier,
        };

        await channelChatCache
            .InsertAsync(cache, TimeSpan.FromDays(1))
            .ConfigureAwait(false);
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ChatTradeRequest request)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ChatWhisperRequest request)
    {
        return ValueTask.CompletedTask;
    }
}
