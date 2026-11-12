using Arch.Core;
using OpenWorker.Channel;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Channels.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Hotspot.Modules.Trade.Responses;

namespace OpenWorker.DistrictServer.Trade;

/// <summary>
/// The trades of this district: one <see cref="TradeSession"/> per pair of players, found from either player. Both players
/// stand in one channel of the district; a trade whose other side has left the channel is cancelled at its next step.
/// Everything runs on the world loop: no locks, and no <c>await</c> between a check and the change it allows. The exchange
/// itself is one synchronous block (<see cref="Confirm"/>) that checks both sides again and then moves everything.
/// </summary>
public sealed partial class Trades(
    World world,
    ServiceChannels channels,
    TradeGoods goods,
    IPersonFlush persons,
    TimeProvider time,
    ILogger<Trades> logger)
{
    /// <summary>
    /// How long an invitation waits for its answer.
    /// </summary>
    public static TimeSpan InvitationLifetime { get; } = TimeSpan.FromSeconds(99);

    /// <summary>
    /// How many items one side may put on the table.
    /// </summary>
    public const int MaxItems = 5;

    /// <summary>
    /// What a change of the table does with the item.
    /// </summary>
    public const byte AddItem = 1;

    /// <inheritdoc cref="AddItem"/>
    public const byte RemoveItem = 2;

    private readonly Dictionary<Entity, TradeSession> _sessions = [];

    public bool IsTrading(Entity player) => _sessions.ContainsKey(player);

    public TradeSession? SessionOf(Entity player) => _sessions.GetValueOrDefault(player);

    #region Invitation

    /// <summary>
    /// The player asks the one of the name to trade: the other gets the invitation, the asking player nothing until the
    /// answer. Returns the code to answer the asking player with, or zero.
    /// </summary>
    public ushort Invite(Entity player, Entity target)
    {
        if (IsBusy(player))
        {
            return TradeCodes.Busy;
        }

        if (IsBusy(target))
        {
            return TradeCodes.TargetBusy;
        }

        // The client asks for the password before it asks to trade; a server does not take its word for it.
        if (!MayTrade(player))
        {
            return TradeCodes.Refused;
        }

        if (!InSameChannel(player, target))
        {
            return TradeCodes.TargetAway;
        }

        var session = new TradeSession(
            new TradeSide(player, world.Get<ActorComponent>(player)),
            new TradeSide(target, world.Get<ActorComponent>(target)),
            time.GetTimestamp());

        _sessions[player] = session;
        _sessions[target] = session;

        Send(target, new TradeResponse
        {
            Actor = session.Inviter.Actor,
            Name = world.Get<PersonInfoComponent>(player).Name
        });

        return 0;
    }

    /// <summary>
    /// The invited player answers the invitation of <paramref name="inviter"/>.
    /// </summary>
    public void Answer(Entity player, ActorValue inviter, bool accept)
    {
        if (SessionOf(player) is not { IsOpen: false } session || session.Invitee.Player != player ||
            session.Inviter.Actor.Identifier != inviter.Identifier)
        {
            Refuse(player, TradeOpcode.Accept, TradeCodes.Refused);
            return;
        }

        if (!InSameChannel(player, session.Inviter.Player))
        {
            Cancel(session, session.Inviter.Actor, TradeCodes.TargetAway);
            return;
        }

        // A player who has not given its trade password cannot take an invitation: the answer goes out as a refusal.
        accept = accept && MayTrade(player);

        var answer = new TradeAcceptResponse { IsAccept = (byte)(accept ? 1 : 0), Actor = session.Invitee.Actor };

        if (accept)
        {
            session.IsOpen = true;
        }

        else
        {
            End(session);
        }

        SendBoth(session, answer);
    }

    #endregion

    #region The table

    /// <summary>
    /// The player puts an item of its bags on the table or takes it back.
    /// </summary>
    public void UpdateItem(Entity player, byte update, StorageSlot slot)
    {
        if (OpenSessionOf(player, TradeOpcode.UpdateItem) is not { } session)
        {
            return;
        }

        var side = session.SideOf(player);
        var index = side.IndexOf(slot.Storage, slot.Slot);
        ItemInstance item;

        switch (update)
        {
            case AddItem:
            {
                var code = goods.Offerable(player, slot.Storage, slot.Slot, out item);

                if (code == 0 && index >= 0)
                {
                    code = TradeCodes.AlreadyOffered;
                }

                if (code == 0 && side.Items.Count >= MaxItems)
                {
                    code = TradeCodes.ItemsUnavailable;
                }

                if (code != 0)
                {
                    Refuse(player, TradeOpcode.UpdateItem, code);
                    return;
                }

                side.Items.Add(TradeGoods.Describe(item));
                break;
            }

            case RemoveItem when index >= 0:
            {
                var offer = side.Items[index];

                side.Items.RemoveAt(index);

                // The item may have gone from the slot since it was offered; the client finds it by its serial.
                item = goods.Inventory.InventoryOf(player)?.At(offer.Storage, offer.Slot) is { } held && held.Serial == offer.Serial
                    ? held
                    : new ItemInstance { Serial = offer.Serial, Item = offer.Item, Count = offer.Count };
                break;
            }

            default:
            {
                Refuse(player, TradeOpcode.UpdateItem, TradeCodes.ItemRefused);
                return;
            }
        }

        Unlock(session, session.OtherOf(player));

        SendBoth(session, new TradeUpdateItemResponse
        {
            Actor = side.Actor,
            UpdateType = update,
            Item = item.ToValue(),
            Sockets = [],
            Broach = new ItemBroachValue { Serial = 0, Items = new int[ItemBroachValue.ItemCount] }
        });
    }

    /// <summary>
    /// The player says how much gold it puts on the table.
    /// </summary>
    public void UpdateMoney(Entity player, long money)
    {
        if (OpenSessionOf(player, TradeOpcode.UpdateMoney) is not { } session)
        {
            return;
        }

        var side = session.SideOf(player);
        var other = session.OtherOf(player);

        if (money < 0 || money > goods.Inventory.Gold(player))
        {
            Refuse(player, TradeOpcode.UpdateMoney, TradeCodes.NotEnoughGold);
            return;
        }

        if (!goods.CanReceive(other.Player, money))
        {
            Refuse(player, TradeOpcode.UpdateMoney, TradeCodes.TooMuchGold);
            return;
        }

        side.Money = money;

        Unlock(session, other);

        SendBoth(session, new TradeUpdateMoneyResponse { Actor = side.Actor, Money = money });
    }

    /// <summary>
    /// The player locks its offer, or takes the lock back: then neither side is locked any more.
    /// </summary>
    public void Check(Entity player, bool locked)
    {
        if (SessionOf(player) is not { IsOpen: true } session)
        {
            Refuse(player, TradeOpcode.Check, TradeCodes.Refused);
            return;
        }

        var side = session.SideOf(player);
        var other = session.OtherOf(player);

        if (!InSameChannel(player, other.Player))
        {
            Cancel(session, other.Actor, TradeCodes.TargetAway);
            return;
        }

        if (side.Confirmed || other.Confirmed)
        {
            Refuse(player, TradeOpcode.Check, TradeCodes.Refused);
            return;
        }

        if (locked)
        {
            side.Locked = true;
        }

        else
        {
            side.Locked = false;
            other.Locked = false;
        }

        SendBoth(session, new TradeCheckResponse { Actor = side.Actor, IsChecked = (byte)(locked ? 1 : 0) });
    }

    /// <summary>
    /// The player confirms the trade. The first confirmation is shown to both; the second one makes the exchange.
    /// </summary>
    public ValueTask Confirm(Entity player)
    {
        if (SessionOf(player) is not { IsOpen: true } session || !session.Inviter.Locked || !session.Invitee.Locked)
        {
            Refuse(player, TradeOpcode.Confirm, TradeCodes.Refused);
            return ValueTask.CompletedTask;
        }

        var side = session.SideOf(player);
        var other = session.OtherOf(player);

        if (!InSameChannel(player, other.Player))
        {
            Cancel(session, other.Actor, TradeCodes.TargetAway);
            return ValueTask.CompletedTask;
        }

        if (side.Confirmed)
        {
            return ValueTask.CompletedTask;
        }

        side.Confirmed = true;

        if (!other.Confirmed)
        {
            SendBoth(session, new TradeConfirmResponse());
            return ValueTask.CompletedTask;
        }

        return Exchange(session) ? FlushAsync(session) : ValueTask.CompletedTask;
    }

    /// <summary>
    /// The player closes the trade, with the cause its client gives.
    /// </summary>
    public void Cancel(Entity player, int cause)
    {
        if (SessionOf(player) is { } session)
        {
            Cancel(session, session.SideOf(player).Actor, cause);
        }
    }

    #endregion

    #region Leaving and waiting

    /// <summary>
    /// The connection of the player closes: its trade ends and the other side is told.
    /// </summary>
    public void Leave(Entity player)
    {
        if (SessionOf(player) is { } session)
        {
            Cancel(session, session.SideOf(player).Actor, TradeCodes.LoggedOut);
        }
    }

    /// <summary>
    /// Cancels the invitations nobody answered in time. Called on the world loop by <see cref="TradeExpiryService"/>.
    /// </summary>
    public void Expire()
    {
        if (_sessions.Count == 0)
        {
            return;
        }

        var expired = _sessions.Values
            .Where(e => !e.IsOpen && time.GetElapsedTime(e.InvitedAt) >= InvitationLifetime)
            .Distinct()
            .ToList();

        foreach (var session in expired)
        {
            Cancel(session, default, TradeCodes.Expired);
        }
    }

    #endregion

    #region The exchange

    /// <summary>
    /// Both sides have confirmed: checks everything again, then moves the items and the gold of both sides at once, or
    /// nothing. No <c>await</c> inside, so nothing else on the loop runs between the checks and the move.
    /// </summary>
    /// <returns>Whether something moved, so the players are to be written.</returns>
    private bool Exchange(TradeSession session)
    {
        var first = session.Inviter;
        var second = session.Invitee;

        // An item given away, split, used or sold since it was offered.
        // The items change hands as copies under new serials: with none ready nothing is taken from anybody.
        if (!first.Items.All(e => goods.StillHolds(first.Player, e)) || !second.Items.All(e => goods.StillHolds(second.Player, e)) ||
            !goods.Inventory.CanMake(first.Items.Count + second.Items.Count))
        {
            Cancel(session, default, TradeCodes.ItemsUnavailable);
            return false;
        }

        if (goods.Inventory.Gold(first.Player) < first.Money || goods.Inventory.Gold(second.Player) < second.Money ||
            !goods.CanReceive(first.Player, second.Money - first.Money) || !goods.CanReceive(second.Player, first.Money - second.Money))
        {
            Finish(session, TradeResult.NoGold);
            return false;
        }

        if (!goods.HasRoom(first.Player, second.Items, first.Items) || !goods.HasRoom(second.Player, first.Items, second.Items))
        {
            Finish(session, TradeResult.Failed);
            return false;
        }

        // Everything is checked: from here on nothing can fail.
        var fromFirst = goods.Take(first.Player, first.Items);
        var fromSecond = goods.Take(second.Player, second.Items);

        goods.Give(second.Player, fromFirst);
        goods.Give(first.Player, fromSecond);

        MoveGold(first.Player, second.Money - first.Money);
        MoveGold(second.Player, first.Money - second.Money);

        Finish(session, TradeResult.Done);

        LogTraded(logger, first.Actor.Identifier, first.Items.Count, first.Money, second.Actor.Identifier, second.Items.Count, second.Money);

        return true;
    }

    private void MoveGold(Entity player, long amount)
    {
        if (amount != 0)
        {
            goods.Inventory.TryAddGold(player, amount);
        }
    }

    /// <summary>
    /// Writes both players together, so the two halves of the exchange reach the database side by side rather than one
    /// flush interval apart. Everything has moved already; the wait changes nothing of the game.
    /// </summary>
    private async ValueTask FlushAsync(TradeSession session)
    {
        var first = persons.FlushAsync(session.Inviter.Player);
        var second = persons.FlushAsync(session.Invitee.Player);

        await first;
        await second;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// The trade of the player if it is open and the player may still change its offer. A request that comes while the
    /// player has locked its offer ends the trade, as its client never sends one then.
    /// </summary>
    private TradeSession? OpenSessionOf(Entity player, TradeOpcode request)
    {
        if (SessionOf(player) is not { IsOpen: true } session)
        {
            Refuse(player, request, TradeCodes.Refused);
            return null;
        }

        var side = session.SideOf(player);
        var other = session.OtherOf(player);

        if (side.Locked || side.Confirmed || other.Confirmed)
        {
            Cancel(session, side.Actor, TradeCodes.Refused);
            return null;
        }

        if (!InSameChannel(player, other.Player))
        {
            Cancel(session, other.Actor, TradeCodes.TargetAway);
            return null;
        }

        return session;
    }

    /// <summary>
    /// The offer of one side changed: the lock of the other side was given to what it saw before, so it goes.
    /// </summary>
    private void Unlock(TradeSession session, TradeSide side)
    {
        if (!side.Locked)
        {
            return;
        }

        side.Locked = false;

        SendBoth(session, new TradeCheckResponse { Actor = side.Actor, IsChecked = 0 });
    }

    /// <summary>
    /// Whether the player has given the trade password of its account on this login: the one state in which it may start a
    /// trade, take an invitation or open a private shop. A player with no password, one who has not given it and one locked
    /// by wrong ones may not.
    /// </summary>
    public bool MayTrade(Entity player)
    {
        return world.TryGet(player, out TradePasswordComponent password) && password.Allows;
    }

    /// <summary>
    /// A player is busy when it trades or keeps a shop.
    /// </summary>
    private bool IsBusy(Entity player)
    {
        return IsTrading(player) || (world.TryGet<ShopPrivateComponent>(player, out var shop) && shop.Type != 0);
    }

    public bool InSameChannel(Entity player, Entity other)
    {
        if (!world.IsAlive(player) || !world.IsAlive(other) ||
            !world.Has<ChannelMemberComponent>(player) || !world.Has<ChannelMemberComponent>(other))
        {
            return false;
        }

        var channel = channels.Get(player);

        return channel.Contains(player) && channel.Contains(other);
    }

    private void Cancel(TradeSession session, ActorValue by, int cause)
    {
        End(session);
        SendBoth(session, new TradeCancelResponse { Actor = by, Cause = cause });
    }

    private void Finish(TradeSession session, TradeResult result)
    {
        End(session);
        SendBoth(session, new TradeResultResponse { Result = (byte)result });
    }

    private void End(TradeSession session)
    {
        _sessions.Remove(session.Inviter.Player);
        _sessions.Remove(session.Invitee.Player);
    }

    private void Refuse(Entity player, TradeOpcode request, ushort code)
    {
        Send(player, new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.Trade, request), code));
    }

    private void SendBoth<T>(TradeSession session, T message) where T : IResponseHotspotMessage
    {
        Send(session.Inviter.Player, message);
        Send(session.Invitee.Player, message);
    }

    private void Send<T>(Entity player, T message) where T : IResponseHotspotMessage
    {
        if (world.IsAlive(player) && world.Has<ServerSessionComponent>(player))
        {
            world.Get<ServerSessionComponent>(player).Send(message);
        }
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Trade done: actor {First} gave {FirstItems} items and {FirstGold} gold, actor {Second} gave {SecondItems} items and {SecondGold} gold.")]
    private static partial void LogTraded(ILogger logger, int first, int firstItems, long firstGold, int second, int secondItems, long secondGold);

    #endregion
}
