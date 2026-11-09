using Arch.Core;
using OpenWorker.Channel;
using OpenWorker.Channel.Community;
using OpenWorker.DistrictServer.Trade;
using OpenWorker.Domain.Components;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Hotspot.Modules.Trade.Requests;
using OpenWorker.Hotspot.Modules.Trade.Responses;

namespace OpenWorker.DistrictServer.Services;

/// <summary>
/// The requests of the trade group: a trade between two players of a channel (<see cref="Trades"/>). A trade is a matter of a district: in a maze every player is alone for now, so the maze
/// has no handler and the requests do nothing there.
/// </summary>
public sealed class TradeService(
    World world,
    ServiceChannels channels,
    Trades trades,
    IPersonRelations relations,
    HandlerExecutor errors) :
    IHotspotHandler<TradeRequest>,
    IHotspotHandler<TradeAcceptRequest>,
    IHotspotHandler<TradeCancelRequest>,
    IHotspotHandler<TradeUpdateItemRequest>,
    IHotspotHandler<TradeUpdateMoneyRequest>,
    IHotspotHandler<TradeCheckRequest>,
    IHotspotHandler<TradeConfirmRequest>
{
    /// <summary>
    /// The player asks the player of the name to trade. The other player has to stand in the same channel of this district.
    /// </summary>
    public async ValueTask OnHandleAsync(ServiceHandleContext context, TradeRequest request)
    {
        var player = context.Player;

        if (!FindByName(request.TargetName, out var target))
        {
            Refuse(player, TradeCodes.NameUnknown);
            return;
        }

        if (target == player)
        {
            Refuse(player, TradeCodes.Refused);
            return;
        }

        var me = world.Get<ActorComponent>(player);
        var other = world.Get<ActorComponent>(target);

        if (await relations.IsBlockedAsync(me.Identifier, other.Identifier))
        {
            Refuse(player, TradeCodes.Blocked);
            return;
        }

        if (await relations.IsBlockedAsync(other.Identifier, me.Identifier))
        {
            // The other player does not want to hear from this one, and is not told.
            Send(player, new TradeCancelResponse { Actor = other, Cause = 0 });
            return;
        }

        // Every await is a door: the two may have left, changed channel or begun another trade since.
        if (!world.IsAlive(player))
        {
            return;
        }

        if (!world.IsAlive(target) || world.Get<ActorComponent>(target).Identifier != other.Identifier)
        {
            Refuse(player, TradeCodes.TargetAway);
            return;
        }

        if (trades.Invite(player, target) is var code and not 0)
        {
            Refuse(player, code);
        }
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, TradeAcceptRequest request)
    {
        trades.Answer(context.Player, request.Target, request.IsAccept != 0);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, TradeCancelRequest request)
    {
        trades.Cancel(context.Player, request.Cause);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, TradeUpdateItemRequest request)
    {
        if (request.UpdateType is not (Trades.AddItem or Trades.RemoveItem))
        {
            errors.Charge(context.Player, request.Opcode, HandlerErrorReason.Violation);
        }

        trades.UpdateItem(context.Player, request.UpdateType, request.Slot);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, TradeUpdateMoneyRequest request)
    {
        if (request.Money < 0)
        {
            errors.Charge(context.Player, request.Opcode, HandlerErrorReason.Violation);
        }

        trades.UpdateMoney(context.Player, request.Money);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, TradeCheckRequest request)
    {
        trades.Check(context.Player, request.IsChecked != 0);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnHandleAsync(ServiceHandleContext context, TradeConfirmRequest request)
    {
        return trades.Confirm(context.Player);
    }

    /// <summary>
    /// The player of the name on this district, in any of its channels; whether it shares the channel is the trade's to say.
    /// </summary>
    private bool FindByName(string name, out Entity found)
    {
        found = Entity.Null;

        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        foreach (var channel in channels)
        {
            var match = Entity.Null;

            channel.ForEach(e =>
            {
                if (match == Entity.Null && world.TryGet<PersonInfoComponent>(e, out var info) &&
                    string.Equals(info.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    match = e;
                }
            });

            if (match != Entity.Null)
            {
                found = match;
                return true;
            }
        }

        return false;
    }

    private void Refuse(Entity player, ushort code)
    {
        Send(player, new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.Trade, TradeOpcode.Request), code));
    }

    private void Send<T>(Entity player, T message) where T : Hotspot.Messages.Abstractions.IResponseHotspotMessage
    {
        if (world.IsAlive(player) && world.Has<ServerSessionComponent>(player))
        {
            world.Get<ServerSessionComponent>(player).Send(message);
        }
    }
}
