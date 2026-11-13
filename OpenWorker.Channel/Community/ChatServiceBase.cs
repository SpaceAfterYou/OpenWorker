using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.League.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Chat.Enums;
using OpenWorker.Hotspot.Modules.Chat.Requests;
using OpenWorker.Hotspot.Modules.Chat.Responses;
using OpenWorker.Hotspot.Modules.System.Responses;

namespace OpenWorker.Channel.Community;

/// <summary>
/// What people say to each other. Who hears it depends on how it was said: the ones close by in the channel, the party,
/// the league, one person by name, or everybody on every server.
/// </summary>
public abstract class ChatServiceBase(
    World world,
    CommunityNetwork network,
    ChatThrottle throttle,
    IPartyRoster parties,
    ILeagueRoster leagues,
    IPersonRelations relations,
    IMegaphoneSource megaphones) :
    IHotspotHandler<ChatNormalRequest>,
    IHotspotHandler<ChatWhisperRequest>,
    IHotspotHandler<ChatTradeRequest>,
    IHotspotHandler<ChatMegaphoneRequest>,
    IHotspotHandler<ChatStuffRequest>
{
    /// <summary>
    /// What the client shows for a failed whisper, see the strings of the client.
    /// </summary>
    public static class WhisperError
    {
        /// <summary>The person is not on any server.</summary>
        public const ushort NotOnline = 55700;

        /// <summary>The words were meant for the sender.</summary>
        public const ushort ToSelf = 55701;
    }

    /// <summary>
    /// These two travel inside the answer rather than as an error of their own.
    /// </summary>
    public static class WhisperResult
    {
        /// <summary>The sender has the receiver in the block list.</summary>
        public const int Blocked = 55116;

        /// <summary>The receiver does not take whispers from the sender.</summary>
        public const int Refused = 59201;
    }

    private const int MaxMessageLength = 255;

    /// <summary>
    /// Sends to the ones who are close to the player on this server, the player too.
    /// </summary>
    protected abstract void SendNearby<T>(Entity player, in T message) where T : IResponseHotspotMessage;

    /// <summary>
    /// Runs a command written in the chat. Returns whether the text was one.
    /// </summary>
    protected abstract ValueTask<bool> TryCommandAsync(Entity player, string text);

    /// <summary>
    /// Where the words were said, for a record of them.
    /// </summary>
    protected virtual ValueTask OnNearbyChatAsync(Entity player, string message, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, ChatStuffRequest request)
    {
        // The player can be gone by the time the command has run.
        if (!await TryCommandAsync(context.Player, request.Message) || !world.IsAlive(context.Player))
        {
            return;
        }

        var actor = world.Get<ActorComponent>(context.Player);

        world.Get<ServerSessionComponent>(context.Player).Send(new ChatNormalResponse
        {
            Actor = actor,
            Appearance = ChatMessageAppearance.System,
            Message = Fit($"[CMD EXEC] {request.Message}")
        });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, ChatNormalRequest request)
    {
        var player = context.Player;
        var actor = world.Get<ActorComponent>(player);
        var message = Clean(request.Message);

        if (message.Length == 0 || !throttle.TryAcquire(actor.Identifier))
        {
            return;
        }

        var response = new ChatNormalResponse { Actor = actor, Appearance = request.Appearance, Message = message };

        switch (request.Appearance)
        {
            case ChatMessageAppearance.Normal:
                SendNearby(player, response);
                await OnNearbyChatAsync(player, message, context.CancellationToken);
                break;

            case ChatMessageAppearance.Party:
                // Somebody without a party only talks to themselves.
                await ToWithSenderAsync(actor.Identifier, await parties.MembersAsync(actor.Identifier), response);
                break;

            case ChatMessageAppearance.League:
                var league = world.Get<LeagueComponent>(player).Id;

                if (league != 0)
                {
                    await ToWithSenderAsync(actor.Identifier, await leagues.MembersAsync(league), response);
                }

                break;
        }
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, ChatTradeRequest request)
    {
        var actor = world.Get<ActorComponent>(context.Player);
        var message = Clean(request.Message);

        if (message.Length != 0 && throttle.TryAcquire(actor.Identifier))
        {
            SendNearby(context.Player, new ChatTradeResponse
            {
                Sender = world.Get<PersonInfoComponent>(context.Player).Name,
                Message = message
            });
        }

        return ValueTask.CompletedTask;
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, ChatWhisperRequest request)
    {
        var player = context.Player;
        var me = world.Get<PersonInfoComponent>(player).Name;
        var person = world.Get<ActorComponent>(player).Identifier;
        var session = world.Get<ServerSessionComponent>(player);

        var receiver = request.Receiver.Trim();
        var message = Clean(request.Message);

        if (receiver.Length == 0 || message.Length == 0 || !throttle.TryAcquire(person))
        {
            return;
        }

        if (string.Equals(receiver, me, StringComparison.OrdinalIgnoreCase))
        {
            session.Send(new ErrorMessageResponse(WhisperOpcode, WhisperError.ToSelf));
            return;
        }

        var target = await network.FindByNameAsync(receiver);

        if (target is null)
        {
            // The player can be gone by the time Redis answers.
            if (world.IsAlive(player))
            {
                session.Send(new ErrorMessageResponse(WhisperOpcode, WhisperError.NotOnline));
            }

            return;
        }

        var result = await DecideAsync(person, target);

        if (!world.IsAlive(player))
        {
            return;
        }

        var response = new ChatWhisperResponse
        {
            Sender = me,
            Receiver = target.Name,
            Message = message,
            Result = result
        };

        // The one who is refused is told alone, the other never hears of it.
        if (result != 0)
        {
            session.Send(response);
            return;
        }

        await network.SendAsync([person, target.Person], response);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, ChatMegaphoneRequest request)
    {
        var player = context.Player;
        var actor = world.Get<ActorComponent>(player);
        var message = Clean(request.Message);

        if (message.Length == 0 || !throttle.TryAcquire(actor.Identifier))
        {
            return;
        }

        var error = await megaphones.TakeAsync(player, request.Item.Storage, request.Item.Slot);

        if (!world.IsAlive(player))
        {
            return;
        }

        if (error != 0)
        {
            world.Get<ServerSessionComponent>(player).Send(new ErrorMessageResponse(MegaphoneOpcode, error));
            return;
        }

        // The name and the number come from the server, not from what the client wrote.
        await network.BroadcastAsync(new ChatMegaphoneResponse
        {
            Storage = request.Item.Storage,
            Slot = request.Item.Slot,
            Person = actor.Identifier,
            Name = world.Get<PersonInfoComponent>(player).Name,
            Message = message
        });
    }

    private static MessageOpcode WhisperOpcode { get; } = new((byte)GroupOpcode.Chat, (byte)ChatOpcode.Whisper);
    private static MessageOpcode MegaphoneOpcode { get; } = new((byte)GroupOpcode.Chat, (byte)ChatOpcode.Megaphone);

    /// <summary>
    /// Zero when the whisper goes through, otherwise the string the client shows the sender.
    /// </summary>
    private async ValueTask<int> DecideAsync(int sender, PresenceRecord target)
    {
        if (await relations.IsBlockedAsync(sender, target.Person))
        {
            return WhisperResult.Blocked;
        }

        return target.Whisper switch
        {
            PersonOptions.Nobody => WhisperResult.Refused,
            PersonOptions.OnlyFriends when !await relations.IsFriendAsync(target.Person, sender) => WhisperResult.Refused,
            _ => 0
        };
    }

    /// <summary>
    /// The sender hears it too, even when nobody else is there to.
    /// </summary>
    private ValueTask ToWithSenderAsync<T>(int sender, IReadOnlyList<int> others, T message) where T : IResponseHotspotMessage
    {
        return network.SendAsync(others.Append(sender), message);
    }

    private static string Clean(string value)
    {
        var text = value.Trim();

        return text.Length > MaxMessageLength || text.Contains('\0') ? string.Empty : text;
    }

    private static string Fit(string value)
    {
        return value.Length > MaxMessageLength ? value[..MaxMessageLength] : value;
    }
}
