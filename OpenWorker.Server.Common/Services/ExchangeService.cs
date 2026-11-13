using System.Collections.ObjectModel;
using Arch.Core;
using Microsoft.Extensions.Logging;
using OpenWorker.Channel.Community;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Boosters;
using OpenWorker.Gameplay.Modules.Exchange;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Post;
using OpenWorker.Gameplay.Modules.Progress;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Exchange.Requests;
using OpenWorker.Hotspot.Modules.Exchange.Responses;
using OpenWorker.Hotspot.Modules.Exchange.Types;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.Post.Responses;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Server.Common.Services;

/// <summary>
/// The exchange, in a district and in a maze: the client asks for the lists at every entry, and the window can be opened
/// anywhere. A player gives some of an item to wait there (it leaves the bags), puts a waiting listing on sale at a price
/// (paying the fee of its kind), takes a listing back (it returns by letter), searches the listings of everybody and buys.
/// Listings live in the database (<see cref="IExchangeStore"/>), shared by every server; a purchase is decided there by one
/// statement, the item goes to the buyer and the gold to the seller by letter, and the seller is told wherever it plays.
/// </summary>
/// <remarks>
/// What leaves a player (an item given, a fee, the price of a purchase) is taken in memory and written to the database
/// (<see cref="IPersonCommit"/>) before the store is asked for what it pays for; it is given back when that write, or the
/// store, refuses or fails, and the request is answered with its error code. A crash after the write loses what was paid; it
/// never leaves an item both in the bags and on sale, nor a purchase unpaid. The tickets of a sale are spent after the sale
/// is written, as they are spent only when it is.
/// </remarks>
public sealed partial class ExchangeService(
    World world,
    IExchangeStore store,
    IPostStore post,
    InventoryService inventory,
    CommunityNetwork network,
    IPersonFlush persons,
    IPersonCommit commit,
    ReadOnlyCollection<ItemClassifyFeeRow> fees,
    ILogger<ExchangeService> logger,
    BoosterManager? boosters = null,
    AchievementManager? achievements = null,
    TimeProvider? time = null
) :
    IHotspotHandler<ExchangeSearchRequest>,
    IHotspotHandler<ExchangePriceHistoryRequest>,
    IHotspotHandler<ExchangeInterestListRequest>,
    IHotspotHandler<ExchangeInterestItemRequest>,
    IHotspotHandler<ExchangeWaitRegisterRequest>,
    IHotspotHandler<ExchangeSellRegisterRequest>,
    IHotspotHandler<ExchangeItemBuyRequest>,
    IHotspotHandler<ExchangeItemRecallRequest>,
    IHotspotHandler<ExchangeMyListRequest>
{
    private readonly RowIndex<int, ItemClassifyFeeRow> _fees = fees.IndexBy(e => e.Id);

    private DateTime Now => (time ?? TimeProvider.System).GetUtcNow().UtcDateTime;

    #region The lists the client asks for at every entry

    public async ValueTask OnHandleAsync(ServiceHandleContext context, ExchangeInterestListRequest request)
    {
        var me = Me(context);
        var items = await store.ListInterestsAsync(me, context.CancellationToken);

        Send(context, new ExchangeInterestListResponse { Person = me, Items = items });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, ExchangeMyListRequest request)
    {
        var listings = await store.ListMineAsync(Me(context), context.CancellationToken);

        Send(context, new ExchangeMyListResponse { Items = listings.Select(ExchangeListings.Mine).ToArray() });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, ExchangeInterestItemRequest request)
    {
        var me = Me(context);

        if (!inventory.Catalog.Exists(request.Item))
        {
            Send(context, new ExchangeInterestItemResponse { Person = me, Item = request.Item, Result = ExchangeCodes.NoItem, Add = request.Add });
            return;
        }

        var result = 0;

        if (request.Add != 0)
        {
            var outcome = await store.AddInterestAsync(me, request.Item, ExchangeRules.InterestLimit, context.CancellationToken);

            result = outcome == ExchangeInterestOutcome.Full ? ExchangeCodes.InterestFull : 0;
        }
        else
        {
            await store.RemoveInterestAsync(me, request.Item, context.CancellationToken);
        }

        Send(context, new ExchangeInterestItemResponse { Person = me, Item = request.Item, Result = result, Add = request.Add });
    }

    #endregion

    #region Selling

    public async ValueTask OnHandleAsync(ServiceHandleContext context, ExchangeWaitRegisterRequest request)
    {
        var player = context.Player;

        if (Refusal(player, request) is { } code)
        {
            Send(context, new ExchangeWaitRegisterResponse { Result = code });
            return;
        }

        var item = inventory.InventoryOf(player)!.At((StorageGroup)request.Storage, request.Slot)!;
        inventory.Catalog.TryGet(request.Item, out var row);
        var classify = inventory.Catalog.Classify(request.Item)!.Value;
        var (level, useClass) = inventory.Catalog.Limits(request.Item);
        var info = world.Get<PersonInfoComponent>(player);
        var account = Account(context);

        var draft = new ExchangeListingDraft(
            Me(context),
            info.Name,
            (byte)info.Hero,
            PostLetters.Attach(item, request.Count),
            classify.Group,
            classify.SubGroup,
            useClass,
            level,
            row.Rank);

        // Taken, and written, before the listing is: a listing never exists while its item is still in the bags.
        inventory.Spend(player, item, request.Count);

        ExchangeListingResult? result = null;

        try
        {
            result = await PaidAsync(context, ExchangeOpcode.WaitRegister,
                () => store.AddWaitingAsync(draft, ExchangeRules.WaitingLimit, context.CancellationToken));
        }
        finally
        {
            if (result is not { Outcome: ExchangeOutcome.Done, Listing: not null })
            {
                await GiveBackAsync(context, account, 0, [draft.Goods]);
            }
        }

        if (result is not { Outcome: ExchangeOutcome.Done, Listing: { } listing })
        {
            Send(context, new ExchangeWaitRegisterResponse { Result = result is null ? ExchangeCodes.ItemError : ExchangeCodes.WaitingFull });
            return;
        }

        Send(context, new ExchangeWaitRegisterResponse { Result = 0 });
        Send(context, new ExchangeMyListResponse { Items = [ExchangeListings.Mine(listing)] });

        await FlushAsync(context);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, ExchangeSellRegisterRequest request)
    {
        var player = context.Player;
        var me = Me(context);

        if (!MayTrade(player))
        {
            Send(context, new ExchangeSellRegisterResponse { Result = ExchangeCodes.Mismatch });
            return;
        }

        if (request.Price <= 0)
        {
            Send(context, new ExchangeSellRegisterResponse { Result = ExchangeCodes.NoPrice });
            return;
        }

        var mine = await store.ListMineAsync(me, context.CancellationToken);
        var listing = mine.Find(e => e.Serial == request.Serial);

        if (!world.IsAlive(player))
        {
            return;
        }

        if (listing is null)
        {
            Send(context, new ExchangeSellRegisterResponse { Result = ExchangeCodes.Gone });
            return;
        }

        if (listing.State != ExchangeListingState.Waiting)
        {
            Send(context, new ExchangeSellRegisterResponse { Result = ExchangeCodes.ItemError });
            return;
        }

        if (request.Price > ExchangeRules.TotalPriceLimit || request.Price * listing.Count > ExchangeRules.TotalPriceLimit)
        {
            Send(context, new ExchangeSellRegisterResponse { Result = ExchangeCodes.PriceTooHigh });
            return;
        }

        var fee = FeeOf(listing.Item);

        if (inventory.Gold(player) < fee)
        {
            Send(context, new ExchangeSellRegisterResponse { Result = ExchangeCodes.NotEnoughGold });
            return;
        }

        // The tickets go only where they change something, as in the original.
        var boosterSteps = (int)Booster(player, BoosterEffect.IncExchangeExpire);
        var freeBooster = Booster(player, BoosterEffect.FreeExchangeCommission) > 0;
        var useCommission = request.CommissionTicket != 0 && !freeBooster;
        var useExpire = request.ExpireTicket != 0 && boosterSteps < ExchangeRules.BoosterExtensionEnough;
        var onSale = mine.Count(e => e.State == ExchangeListingState.OnSale);
        var limit = ExchangeRules.SaleLimit + (int)Booster(player, BoosterEffect.IncExchangeCount);
        var useCount = request.CountTicket != 0 && onSale >= limit;

        if ((useCommission && !Holds(player, ExchangeRules.CommissionTicket)) ||
            (useCount && !Holds(player, ExchangeRules.CountTicket)) ||
            (useExpire && !Holds(player, ExchangeRules.ExpireTicket)))
        {
            Send(context, new ExchangeSellRegisterResponse { Result = ExchangeCodes.ItemError });
            return;
        }

        if (fee > 0 && !inventory.TryAddGold(player, -fee))
        {
            Send(context, new ExchangeSellRegisterResponse { Result = ExchangeCodes.NotEnoughGold });
            return;
        }

        var rate = ExchangeRules.RateOf(useCommission, freeBooster, Booster(player, BoosterEffect.ReduceExchangeCommission));
        var expiresAt = Now + ExchangeRules.DurationOf(boosterSteps, useExpire);
        var account = Account(context);

        // The fee is written before the sale is: a sale never exists while its fee is still in the purse.
        ExchangeListingResult? result = null;

        try
        {
            result = await PaidAsync(context, ExchangeOpcode.SellRegister, () => store.PutOnSaleAsync(
                request.Serial,
                me,
                request.Price,
                rate,
                expiresAt,
                useCount ? ExchangeRules.SaleLimitWithTicket : limit,
                context.CancellationToken));
        }
        finally
        {
            if (result is not { Outcome: ExchangeOutcome.Done } && fee > 0)
            {
                await GiveBackAsync(context, account, fee, []);
            }
        }

        if (result is not { Outcome: ExchangeOutcome.Done, Listing: { } listed })
        {
            Send(context, new ExchangeSellRegisterResponse
            {
                Result = result?.Outcome switch
                {
                    ExchangeOutcome.Full => ExchangeCodes.SaleFull,
                    ExchangeOutcome.Missing => ExchangeCodes.Gone,
                    _ => ExchangeCodes.ItemError
                }
            });
            return;
        }

        if (!world.IsAlive(player))
        {
            return;
        }

        // The tickets used are spent once the sale is written.
        Spend(player, useCommission, ExchangeRules.CommissionTicket);
        Spend(player, useCount, ExchangeRules.CountTicket);
        Spend(player, useExpire, ExchangeRules.ExpireTicket);

        Send(context, new ExchangeSellRegisterResponse { Result = 0 });
        Send(context, new ExchangeMyListResponse { Items = [ExchangeListings.Mine(listed)] });

        await FlushAsync(context);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, ExchangeItemRecallRequest request)
    {
        var me = Me(context);
        var account = Account(context);
        var now = Now;

        var result = await store.RecallAsync(request.Serial, me, listing => ExchangeListings.ReturnLetter(listing, now), context.CancellationToken);

        if (result.Outcome != ExchangeOutcome.Done || result.Letter is null)
        {
            Send(context, new ExchangeItemRecallResponse { Result = ExchangeCodes.Gone, Serial = request.Serial });
            return;
        }

        Send(context, new ExchangeItemRecallResponse { Result = 0, Serial = request.Serial });

        await TellLetterAsync(me, account, result.Letter, context.CancellationToken);
    }

    #endregion

    #region Buying

    public async ValueTask OnHandleAsync(ServiceHandleContext context, ExchangeSearchRequest request)
    {
        if (request.Item > 0 && !inventory.Catalog.Exists(request.Item))
        {
            Fail(context, ExchangeOpcode.Search, ExchangeCodes.NoItem);
            return;
        }

        // A page so far that its first row cannot be counted in an int is as empty as any page past the end.
        var page = Math.Clamp(request.Page, 1, int.MaxValue / ExchangeRules.PageSize);

        var search = ExchangeRules.Filter(request.Item, request.Category, request.SubCategory, request.UseClass, request.LevelMin,
            request.LevelMax, request.GradeMin, request.GradeMax, request.Price, request.Upgrade);

        var (total, found) = await store.SearchAsync(search, page, ExchangeRules.PageSize, Now, context.CancellationToken);

        Send(context, new ExchangeSearchResponse
        {
            Page = page,
            PageMax = ExchangeRules.PagesOf(total),
            Items = found.Select(ExchangeListings.Found).ToArray()
        });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, ExchangePriceHistoryRequest request)
    {
        var sales = await store.ListSalesAsync(request.Item, ExchangeRules.SalesShown, context.CancellationToken);

        Send(context, new ExchangePriceHistoryResponse
        {
            Person = Me(context),
            Item = request.Item,
            Sales = sales.Select(ExchangeListings.Sale).ToArray()
        });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, ExchangeItemBuyRequest request)
    {
        var player = context.Player;
        var me = Me(context);

        // The trade password is asked before anything of the request is looked at.
        if (!MayTrade(player))
        {
            Bought(context, request, ExchangeCodes.Mismatch);
            return;
        }

        if (request.Count <= 0 || request.Count > inventory.Catalog.StackLimit(request.Item))
        {
            Bought(context, request, ExchangeCodes.CountMismatch);
            return;
        }

        var listing = await store.FindAsync(request.Exchange, context.CancellationToken);

        if (!world.IsAlive(player))
        {
            return;
        }

        var now = Now;

        var refusal = listing is null ? ExchangeCodes.Gone
            : listing.SellerId == me ? ExchangeCodes.OwnItem
            : listing.State != ExchangeListingState.OnSale || listing.Item != request.Item ? ExchangeCodes.Mismatch
            : listing.ExpiresAt <= now ? ExchangeCodes.Expired
            : listing.Count < request.Count ? ExchangeCodes.CountMismatch
            : (int?)null;

        if (refusal is { } code)
        {
            Bought(context, request, code);
            return;
        }

        var priceOne = listing!.PriceOne;
        var total = priceOne * request.Count;

        if (!inventory.TryAddGold(player, -total))
        {
            Bought(context, request, ExchangeCodes.NotEnoughGold);
            return;
        }

        var info = world.Get<PersonInfoComponent>(player);
        var account = Account(context);

        // The price is written before the purchase is: an item is never bought while its price is still in the purse.
        ExchangeBuyResult? result = null;

        try
        {
            result = await PaidAsync(context, ExchangeOpcode.ItemBuy, () => store.BuyAsync(
                request.Exchange,
                me,
                request.Item,
                request.Count,
                priceOne,
                now,
                (sold, count) => ExchangeListings.SaleLetters(sold, count, me, info.Name, (byte)info.Hero, now),
                context.CancellationToken));
        }
        finally
        {
            if (result is not { Outcome: ExchangeOutcome.Done })
            {
                await GiveBackAsync(context, account, total, []);
            }
        }

        if (result is not { Outcome: ExchangeOutcome.Done, Listing: { } soldFrom, ToBuyer: { } toBuyer, ToSeller: { } toSeller })
        {
            Bought(context, request, result?.Outcome switch
            {
                ExchangeOutcome.Missing => ExchangeCodes.Gone,
                ExchangeOutcome.Own => ExchangeCodes.OwnItem,
                ExchangeOutcome.Expired => ExchangeCodes.Expired,
                ExchangeOutcome.NotEnough => ExchangeCodes.CountMismatch,
                _ => ExchangeCodes.Mismatch
            });
            return;
        }

        LogSold(logger, soldFrom.Id, request.Count, soldFrom.SellerId, me, total);

        Bought(context, request, 0);

        if (world.IsAlive(player))
        {
            achievements?.Count(player, AchievementType.ExchangePurchase);
        }

        await FlushAsync(context);
        await TellLetterAsync(me, account, toBuyer, CancellationToken.None);

        // The seller is told wherever it plays: the listing as it is now, and the letter with the gold.
        var seller = soldFrom.SellerId;

        await network.SendAsync(seller, new ExchangeMyListResponse { Items = [ExchangeListings.Mine(soldFrom)] });
        await TellLetterAsync(seller, 0, toSeller, CancellationToken.None);
    }

    #endregion

    #region Checks

    /// <summary>
    /// Why an item may not be given to the exchange, or nothing when it may.
    /// </summary>
    private int? Refusal(Entity player, ExchangeWaitRegisterRequest request)
    {
        if (!MayTrade(player))
        {
            return ExchangeCodes.Mismatch;
        }

        if (!inventory.Catalog.TryGet(request.Item, out var row) || inventory.Catalog.Classify(request.Item) is not { } classify)
        {
            return ExchangeCodes.NoItem;
        }

        if (request.Count <= 0 || request.Count > Math.Max((short)1, row.Stack))
        {
            return ExchangeCodes.BadCount;
        }

        var storage = (StorageGroup)request.Storage;

        if (!PostLetters.CanSendFrom(storage) || inventory.InventoryOf(player) is not { } bag)
        {
            return ExchangeCodes.WrongStorage;
        }

        var item = bag.At(storage, request.Slot);

        if (item is null || item.Item != request.Item)
        {
            return ExchangeCodes.ItemError;
        }

        // The table rule "bound from the start" counts even for an item that is not bound yet.
        if (ItemBinding.HeldBack(item) != ItemHeldBack.Not || (ItemBindType)row.BindType == ItemBindType.OnPickUp)
        {
            return ExchangeCodes.NotTradable;
        }

        // Gear that is worn down does not go: its slot types are 1 and 2.
        if (classify.SlotType is 1 or 2 && item.Endurance < 100)
        {
            return ExchangeCodes.ItemError;
        }

        return request.Count > item.Count ? ExchangeCodes.BadCount : null;
    }

    /// <summary>
    /// Whether the player has given the trade password of its account on this login, as a trade asks.
    /// </summary>
    private bool MayTrade(Entity player)
    {
        return world.TryGet(player, out TradePasswordComponent password) && password.Allows;
    }

    private long FeeOf(int item)
    {
        return inventory.Catalog.TryGet(item, out var row) && _fees.TryGet(row.Classify, out var fee) ? fee.ExchangeFee : 0;
    }

    private float Booster(Entity player, BoosterEffect effect)
    {
        return boosters?.GetValue(player, effect) ?? 0;
    }

    private bool Holds(Entity player, int item) => inventory.CountOf(player, item) > 0;

    private void Spend(Entity player, bool used, int item)
    {
        if (used)
        {
            inventory.TryTake(player, item, 1);
        }
    }

    #endregion

    #region What moves

    /// <summary>
    /// Writes what was just taken from the player, then asks the store for what it pays for. Null when the write was not
    /// taken or the store failed: nothing was bought or listed, what was taken goes back and the request is refused with its
    /// code. A failure of the database is no fault of the client, so it is answered and not charged to the session; a server
    /// that stops is not answered.
    /// </summary>
    private async ValueTask<T?> PaidAsync<T>(ServiceHandleContext context, ExchangeOpcode request, Func<Task<T>> call) where T : struct
    {
        var me = Me(context);

        try
        {
            if (await commit.CommitAsync(context.Player))
            {
                return await call();
            }

            LogNotWritten(logger, request, me);
        }
        catch (Exception e) when (e is not OperationCanceledException || !context.CancellationToken.IsCancellationRequested)
        {
            LogStoreFailed(logger, e, request, me);
        }

        return null;
    }

    /// <summary>
    /// What was taken from the player goes back when the store refused or failed. A player who has left meanwhile, or whose
    /// bags filled, gets it as a letter to the account, so nothing is lost.
    /// </summary>
    private async ValueTask GiveBackAsync(ServiceHandleContext context, int account, long gold, IReadOnlyList<PostItemPersistent> items)
    {
        var player = context.Player;

        if (world.IsAlive(player))
        {
            if (gold > 0 && inventory.TryAddGold(player, gold))
            {
                gold = 0;
            }

            if (items.Count > 0 && PostLetters.Fits(inventory, player, items))
            {
                var created = PostLetters.Give(inventory, player, items);

                if (created.Count > 0)
                {
                    Send(context, new ItemCreateResponse { List = created.Select(InventoryService.ToStorageValue).ToArray() });
                }

                items = [];
            }
        }

        if (gold == 0 && items.Count == 0)
        {
            return;
        }

        LogReturnedToAccount(logger, account, gold, items.Count);

        await post.SendToAccountAsync(
            new PostAccountDraft(account, ExchangeListings.Sender, "[Exchange] Returned", "What the exchange could not take.", gold, items,
                ExchangeListings.AccountLetterKind, ExchangeListings.TakenBack),
            CancellationToken.None);
    }

    /// <summary>
    /// Tells a person of a letter of the exchange, wherever it plays.
    /// </summary>
    private async ValueTask TellLetterAsync(int person, int account, PostPersistent letter, CancellationToken cancellationToken)
    {
        var (unread, _) = await post.CountUnreadAsync(person, account, cancellationToken);

        await network.SendAsync(person, new PostRecvResponse
        {
            PostCount = (short)Math.Min(unread, short.MaxValue),
            Value = PostLetters.Received(letter)
        });
    }

    private async ValueTask FlushAsync(ServiceHandleContext context)
    {
        if (world.IsAlive(context.Player))
        {
            await persons.FlushAsync(context.Player);
        }
    }

    #endregion

    private int Me(ServiceHandleContext context) => world.Get<ActorComponent>(context.Player).Identifier;

    private int Account(ServiceHandleContext context) => world.Get<ClaimsComponent>(context.Player).Account;

    private void Bought(ServiceHandleContext context, ExchangeItemBuyRequest request, int result)
    {
        Send(context, new ExchangeItemBuyResponse { Result = result, Exchange = request.Exchange, Count = request.Count });
    }

    private void Send<T>(ServiceHandleContext context, T message) where T : IResponseHotspotMessage
    {
        if (world.IsAlive(context.Player))
        {
            world.Get<ServerSessionComponent>(context.Player).Send(message);
        }
    }

    private void Fail(ServiceHandleContext context, ExchangeOpcode command, int code)
    {
        Send(context, new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.Exchange, command), (ushort)code));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Exchange listing {Listing}: {Count} sold by {Seller} to {Buyer} for {Total} gold")]
    private static partial void LogSold(ILogger logger, int listing, short count, int seller, int buyer, long total);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Exchange request {Request} of person {Person} is refused: what it pays could not be written; it is given back")]
    private static partial void LogNotWritten(ILogger logger, ExchangeOpcode request, int person);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Exchange request {Request} of person {Person} failed in the database and is refused; what it paid is given back")]
    private static partial void LogStoreFailed(ILogger logger, Exception exception, ExchangeOpcode request, int person);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The exchange could not give back to its player: {Gold} gold and {Items} items go to the box of account {Account}")]
    private static partial void LogReturnedToAccount(ILogger logger, int account, long gold, int items);
}
