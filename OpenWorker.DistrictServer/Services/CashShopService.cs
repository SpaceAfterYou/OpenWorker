using Arch.Core;
using Microsoft.Extensions.Logging;
using OpenWorker.Channel.Community;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.CashShop;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Gameplay.Modules.Post;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.Post.Responses;
using OpenWorker.Hotspot.Modules.Shop.Requests;
using OpenWorker.Hotspot.Modules.Shop.Responses;
using OpenWorker.Hotspot.Modules.Shop.Types;
using OpenWorker.Hotspot.Modules.System.Responses;

namespace OpenWorker.DistrictServer.Services;

/// <summary>
/// The cash shop: the list of products, purchases, presents, the lists a character keeps and the gacha items bought there.
/// Its currency is the balance of the account, which only a game master gives; nothing is paid for with anything real.
/// </summary>
/// <remarks>
/// A purchase or a present is checked against the tables here (<see cref="CashShopOrders"/>), then decided by the store in
/// one transaction (<see cref="ICashShopStore"/>): the balance is taken only while it holds the price, the limited products
/// are counted only within their limits, and what was bought goes by letter, written with them. A player makes one at a
/// time. A failure of the database is answered with the client's code and is no fault of the client.
/// </remarks>
public sealed partial class CashShopService(
    World world,
    CashShopOrders orders,
    CashShopGacha gacha,
    CashShopWindow window,
    InventoryService inventory,
    ICashShopStore store,
    IPostStore post,
    CommunityNetwork network,
    ILogger<CashShopService> logger,
    TimeProvider? time = null
) :
    IHotspotHandler<ShopCashLoadRequest>,
    IHotspotHandler<ShopCashBuyRequest>,
    IHotspotHandler<ShopCashGiftRequest>,
    IHotspotHandler<ShopCashSetRequest>,
    IHotspotHandler<ShopCashSetDelRequest>,
    IHotspotHandler<ShopGachaRequest>
{
    // The names of the lists the window offers: the basket is 0.
    private const byte LastSet = 8;

    private readonly HashSet<int> _busy = [];

    private TimeProvider Time => time ?? TimeProvider.System;

    #region The window

    public ValueTask OnHandleAsync(ServiceHandleContext context, ShopCashLoadRequest request)
    {
        return window.LoadAsync(context.Player, context.CancellationToken);
    }

    #endregion

    #region Purchases and presents

    public async ValueTask OnHandleAsync(ServiceHandleContext context, ShopCashBuyRequest request)
    {
        var player = context.Player;
        var me = Me(context);
        var info = world.Get<PersonInfoComponent>(player);
        var buyer = new CashBuyer(me, Account(context), info.Name, (byte)info.Hero);

        var (order, code) = orders.Check(request.ItemList, buyer, false, Time.GetLocalNow());

        if (order is null)
        {
            Fail(context, ShopOpcode.CashBuy, code);
            return;
        }

        var letters = CashShopOrders.Letters(order, buyer, null, Now);
        var result = await PayAsync(context, ShopOpcode.CashBuy, buyer, order, letters);

        if (result is not { Outcome: CashPurchaseOutcome.Done } done)
        {
            return;
        }

        LogBought(logger, me, request.ItemList.Count, order.Price, done.Balance);

        Send(context, new ItemUpdateCashResponse { Total = CashShopWindow.Balance(done.Balance) });
        Send(context, new ShopCashBuyResponse { Appearances = [], Failed = [] });

        await TellLettersAsync(me, buyer.Account, letters);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, ShopCashGiftRequest request)
    {
        var player = context.Player;
        var me = Me(context);
        var info = world.Get<PersonInfoComponent>(player);
        var account = Account(context);

        if (request.ItemList.Count is 0 or > CashShopOrders.MaxEntries)
        {
            Fail(context, ShopOpcode.CashGift, CashShopCodes.WrongProduct);
            return;
        }

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > PostPersistent.MaxNameLength)
        {
            Fail(context, ShopOpcode.CashGift, CashShopCodes.NoReceiver);
            return;
        }

        CashReceiver? found;

        try
        {
            found = await store.FindPersonAsync(request.Name, context.CancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException || !context.CancellationToken.IsCancellationRequested)
        {
            LogStoreFailed(logger, e, ShopOpcode.CashGift, me);
            Fail(context, ShopOpcode.CashGift, CashShopCodes.GiftFailed);
            return;
        }

        if (!world.IsAlive(player))
        {
            return;
        }

        if (found is not { } receiver)
        {
            Fail(context, ShopOpcode.CashGift, CashShopCodes.NoReceiver);
            return;
        }

        if (receiver.Person == me)
        {
            Fail(context, ShopOpcode.CashGift, CashShopCodes.GiftFailed);
            return;
        }

        var to = new CashBuyer(receiver.Person, receiver.Account, receiver.Name, receiver.Hero);
        var (order, code) = orders.Check(request.ItemList, to, true, Time.GetLocalNow());

        if (order is null)
        {
            Fail(context, ShopOpcode.CashGift, code);
            return;
        }

        var giver = new CashBuyer(me, account, info.Name, (byte)info.Hero);
        var letters = CashShopOrders.Letters(order, to, info.Name, Now);
        var result = await PayAsync(context, ShopOpcode.CashGift, giver, order, letters);

        if (result is not { Outcome: CashPurchaseOutcome.Done } done)
        {
            return;
        }

        LogGiven(logger, me, receiver.Person, request.ItemList.Count, order.Price, done.Balance);

        Send(context, new ItemUpdateCashResponse { Total = CashShopWindow.Balance(done.Balance) });
        Send(context, new ShopCashGiftResponse { Failed = [] });

        await TellLettersAsync(receiver.Person, receiver.Account, letters);
    }

    /// <summary>
    /// Asks the store to take the price, count the limited products and write the letters. Null when it was refused or
    /// failed, and then the request is answered with its code.
    /// </summary>
    private async ValueTask<CashPurchaseResult?> PayAsync(
        ServiceHandleContext context,
        ShopOpcode command,
        CashBuyer payer,
        CashOrder order,
        List<PostPersistent> letters)
    {
        if (!_busy.Add(payer.Person))
        {
            Fail(context, command, CashShopCodes.Busy);
            return null;
        }

        try
        {
            var result = await store.BuyAsync(new CashPurchaseDraft(payer.Account, payer.Person, order.Price, order.Limits, letters),
                context.CancellationToken);

            if (result.Outcome != CashPurchaseOutcome.Done)
            {
                Send(context, new ItemUpdateCashResponse { Total = CashShopWindow.Balance(result.Balance) });
                Fail(context, command, result.Outcome == CashPurchaseOutcome.NotEnough ? CashShopCodes.NotEnough : CashShopCodes.LimitReached);
                return null;
            }

            return result;
        }
        catch (Exception e) when (e is not OperationCanceledException || !context.CancellationToken.IsCancellationRequested)
        {
            LogStoreFailed(logger, e, command, payer.Person);
            Fail(context, command, command == ShopOpcode.CashGift ? CashShopCodes.GiftFailed : CashShopCodes.Failed);
            return null;
        }
        finally
        {
            _busy.Remove(payer.Person);
        }
    }

    /// <summary>
    /// Tells the receiver of the letters of an order, wherever it plays, that they came.
    /// </summary>
    private async ValueTask TellLettersAsync(int person, int account, List<PostPersistent> letters)
    {
        try
        {
            var (unread, _) = await post.CountUnreadAsync(person, account, CancellationToken.None);

            foreach (var letter in letters)
            {
                await network.SendAsync(person, new PostRecvResponse
                {
                    PostCount = (short)Math.Min(unread, short.MaxValue),
                    Value = PostLetters.Received(letter)
                });
            }
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            LogNoticeFailed(logger, e, person);
        }
    }

    #endregion

    #region The lists of a character

    public async ValueTask OnHandleAsync(ServiceHandleContext context, ShopCashSetRequest request)
    {
        if (request.SetNo > LastSet || request.IndexList.Count != CashSetValue.Size || request.ItemList.Count != CashSetValue.Size)
        {
            Fail(context, ShopOpcode.CashSet, CashShopCodes.WrongProduct);
            return;
        }

        var name = request.Name.Trim();

        var set = new CashShopSetPersistent
        {
            PersonId = Me(context),
            SetNo = request.SetNo,
            Name = name.Length > CashShopSetPersistent.MaxNameLength ? name[..CashShopSetPersistent.MaxNameLength] : name,
            Products = request.IndexList.ToArray(),
            Items = request.ItemList.ToArray()
        };

        try
        {
            await store.SaveSetAsync(set, context.CancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException || !context.CancellationToken.IsCancellationRequested)
        {
            LogStoreFailed(logger, e, ShopOpcode.CashSet, set.PersonId);
            Fail(context, ShopOpcode.CashSet, CashShopCodes.Failed);
            return;
        }

        Send(context, new ShopCashSetResponse { Set = CashShopWindow.Shown(set) });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, ShopCashSetDelRequest request)
    {
        if (request.SetNo is 0 or > LastSet)
        {
            Fail(context, ShopOpcode.CashSetDel, CashShopCodes.WrongProduct);
            return;
        }

        try
        {
            await store.DeleteSetAsync(Me(context), request.SetNo, context.CancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException || !context.CancellationToken.IsCancellationRequested)
        {
            LogStoreFailed(logger, e, ShopOpcode.CashSetDel, Me(context));
            Fail(context, ShopOpcode.CashSetDel, CashShopCodes.Failed);
            return;
        }

        // A list already gone is gone all the same: the window forgets it.
        Send(context, new ShopCashSetDelResponse { SetNo = request.SetNo });
    }

    #endregion

    #region Gacha

    /// <summary>
    /// Opens gacha items of the bags: each one gives one item of its draw, told to the gacha window. Everything happens in
    /// the inventory of the player, which the flush writes.
    /// </summary>
    public async ValueTask OnHandleAsync(ServiceHandleContext context, ShopGachaRequest request)
    {
        var player = context.Player;

        if (request.Count is 0 or > CashShopGacha.MaxCount || !gacha.TryGetGacha(request.Item, out var box))
        {
            Fail(context, ShopOpcode.Gacha, CashShopCodes.NotGacha);
            return;
        }

        if (inventory.CountOf(player, request.Item) < request.Count)
        {
            Fail(context, ShopOpcode.Gacha, CashShopCodes.CountMismatch);
            return;
        }

        if (!CashShopGacha.Open(box, Time.GetLocalNow().DateTime))
        {
            Fail(context, ShopOpcode.Gacha, CashShopCodes.GachaClosed);
            return;
        }

        if (gacha.Draw(box, request.Count) is not { } results)
        {
            Fail(context, ShopOpcode.Gacha, CashShopCodes.GachaFailed);
            return;
        }

        var given = results.Select(e => (e.Item, 1)).ToList();

        await inventory.EnsureSerialsAsync(given);

        // The wait let other work run: the bags are counted again.
        if (!world.IsAlive(player))
        {
            return;
        }

        if (inventory.CountOf(player, request.Item) < request.Count)
        {
            Fail(context, ShopOpcode.Gacha, CashShopCodes.CountMismatch);
            return;
        }

        if (!inventory.CanGiveAll(player, given))
        {
            Fail(context, ShopOpcode.Gacha, CashShopCodes.NoRoom);
            return;
        }

        inventory.TryTake(player, request.Item, request.Count);

        foreach (var (item, count) in given)
        {
            inventory.TryGive(player, item, count);
        }

        Send(context, new ShopGachaResResponse { Results = results });
    }

    #endregion

    private DateTime Now => Time.GetUtcNow().UtcDateTime;

    private int Me(ServiceHandleContext context) => world.Get<ActorComponent>(context.Player).Identifier;

    private int Account(ServiceHandleContext context) => world.Get<ClaimsComponent>(context.Player).Account;

    private void Send<T>(ServiceHandleContext context, T message) where T : IResponseHotspotMessage
    {
        if (world.IsAlive(context.Player))
        {
            world.Get<ServerSessionComponent>(context.Player).Send(message);
        }
    }

    private void Fail(ServiceHandleContext context, ShopOpcode command, int code)
    {
        Send(context, new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.Shop, command), (ushort)code));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Person {Person} bought {Count} products of the cash shop for {Price}; {Balance} left")]
    private static partial void LogBought(ILogger logger, int person, int count, long price, long balance);

    [LoggerMessage(Level = LogLevel.Information, Message = "Person {Person} sent {Count} products of the cash shop to person {Receiver} for {Price}; {Balance} left")]
    private static partial void LogGiven(ILogger logger, int person, int receiver, int count, long price, long balance);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Cash shop request {Request} of person {Person} failed in the database and is refused")]
    private static partial void LogStoreFailed(ILogger logger, Exception exception, ShopOpcode request, int person);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Person {Person} could not be told of the letters of the cash shop")]
    private static partial void LogNoticeFailed(ILogger logger, Exception exception, int person);
}
