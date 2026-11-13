using Arch.Core;
using Microsoft.Extensions.Logging;
using OpenWorker.Channel.Community;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Post;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.Post;
using OpenWorker.Hotspot.Modules.Post.Requests;
using OpenWorker.Hotspot.Modules.Post.Responses;
using OpenWorker.Hotspot.Modules.Post.Types;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Hotspot.Modules.Shop.Enums;

namespace OpenWorker.DistrictServer.Services;

/// <summary>
/// The post of a town: letters between persons, by name, with text, gold and items, and the letters the game leaves for an
/// account. Every request of the client but the count of new letters names the post box it stands at, so the post is a
/// thing of the district; the maze does not serve it.
/// </summary>
/// <remarks>
/// A letter is written by its store when it changes (see <see cref="IPostStore"/>). What it carries moves through
/// <see cref="InventoryService"/>, which marks the inventory and the wallet for the flush. Sending takes the gold and the
/// items and writes the sender (<see cref="IPersonCommit"/>) before the letter is written, and gives them back if that write
/// or the letter fails; taking what a letter carries claims it in the database first, puts the claim back if it cannot be
/// given, and flushes the person after. A crash in between loses what moved, it never doubles it.
/// </remarks>
public sealed partial class PostService(
    World world,
    IPostStore store,
    InventoryService inventory,
    CommunityNetwork network,
    IPersonFlush persons,
    IPersonCommit commit,
    ILogger<PostService> logger
) :
    IHotspotHandler<PostSendListRequest>,
    IHotspotHandler<PostRecvListRequest>,
    IHotspotHandler<PostSaveListRequest>,
    IHotspotHandler<PostAccountListRequest>,
    IHotspotHandler<PostInfoRequest>,
    IHotspotHandler<PostSendRequest>,
    IHotspotHandler<PostReadRequest>,
    IHotspotHandler<PostAccountReadRequest>,
    IHotspotHandler<PostReceiptRequest>,
    IHotspotHandler<PostAccountReceiptRequest>,
    IHotspotHandler<PostSaveRequest>,
    IHotspotHandler<PostSendDelRequest>,
    IHotspotHandler<PostRecvDelRequest>,
    IHotspotHandler<PostAccountDelRequest>,
    IHotspotHandler<PostSendBackRequest>
{
    /// <summary>
    /// The trailing byte of a list: the client refreshes the window when it is not zero.
    /// </summary>
    private const byte Refresh = 1;

    #region Lists

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PostSendListRequest request)
    {
        var letters = await store.ListSentAsync(Me(context), context.CancellationToken);

        Send(context, new PostSendListResponse { Values = letters.Select(e => PostLetters.Sent(e)).ToArray(), Flag = Refresh });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PostRecvListRequest request)
    {
        var letters = await store.ListReceivedAsync(Me(context), saved: false, context.CancellationToken);

        Send(context, new PostRecvListResponse { Values = letters.Select(e => PostLetters.Received(e)).ToArray(), Flag = Refresh });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PostSaveListRequest request)
    {
        var letters = await store.ListReceivedAsync(Me(context), saved: true, context.CancellationToken);

        Send(context, new PostSaveListResponse { Values = letters.Select(e => PostLetters.Received(e)).ToArray(), Flag = Refresh });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PostAccountListRequest request)
    {
        var letters = await store.ListAccountAsync(Account(context), context.CancellationToken);

        Send(context, new PostAccountListResponse { Values = letters.Select(e => PostLetters.Account(e)).ToArray(), Flag = Refresh });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PostInfoRequest request)
    {
        var (mine, account) = await store.CountUnreadAsync(Me(context), Account(context), context.CancellationToken);

        Send(context, new PostInfoResponse { NewPostCount = Count(mine), NewAccountPostCount = Count(account) });
    }

    #endregion

    #region Sending

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PostSendRequest request)
    {
        var player = context.Player;
        var value = request.Value;
        var bag = inventory.InventoryOf(player);

        if (bag is null)
        {
            RefuseSend(context, PostMessages.Deleted);
            return;
        }

        var info = world.Get<PersonInfoComponent>(player);

        if (Refusal(value, info.Name) is { } refusal)
        {
            RefuseSend(context, refusal);
            return;
        }

        // The items to take: each from its own slot, no more than lies there, none that is bound to its owner.
        var taken = new List<(ItemInstance Item, short Count)>();

        foreach (var entry in value.Items)
        {
            if (entry.Count <= 0 || entry.Slot.Storage == StorageGroup.None)
            {
                continue;
            }

            var item = bag.At(entry.Slot.Storage, entry.Slot.Slot);

            if (!PostLetters.CanSendFrom(entry.Slot.Storage) || item is null || entry.Count > item.Count || taken.Exists(e => e.Item == item))
            {
                RefuseSend(context, PostMessages.CannotBeTraded);
                return;
            }

            // The table rule "bound from the start" counts even for an item that is not bound yet; a broken item goes to a shop only.
            var held = ItemBinding.HeldBack(item);
            var boundFromStart = inventory.Catalog.TryGet(item.Item, out var row) && (ItemBindType)row.BindType == ItemBindType.OnPickUp;

            if (held == ItemHeldBack.Broken)
            {
                RefuseSend(context, PostMessages.CannotBeTraded);
                return;
            }

            if (held == ItemHeldBack.Bound || boundFromStart)
            {
                RefuseSend(context, PostMessages.Bound);
                return;
            }

            taken.Add((item, entry.Count));
        }

        var price = value.Money + PostLetters.Postage;

        if (inventory.Gold(player) < price)
        {
            RefuseSend(context, PostMessages.NotEnoughGold);
            return;
        }

        // Taken before the letter is written, so a letter never exists while its gold and items are still with the sender.
        var items = taken.Select(e => PostLetters.Attach(e.Item, e.Count)).ToList();

        inventory.TryAddGold(player, -price);

        foreach (var (item, count) in taken)
        {
            inventory.Spend(player, item, count);
        }

        var draft = new PostDraft(
            Me(context),
            info.Name,
            (byte)info.Hero,
            value.TargetName,
            value.Title,
            value.Message,
            value.Money,
            PostLetters.Postage,
            items);

        // Read now: the sender may be gone when what was taken has to go back.
        var account = Account(context);

        PostSendResult? result = null;

        try
        {
            // What the letter carries is written as gone from the sender before the letter is written: a crash in between
            // loses it, and never leaves it both with the sender and in the letter.
            if (await commit.CommitAsync(player))
            {
                result = await store.SendAsync(draft, context.CancellationToken);
            }

            else
            {
                LogNotWritten(logger, draft.Sender);
            }
        }

        // A failure of the database is no fault of the client: it is answered below, and not charged to the session.
        catch (Exception e) when (e is not OperationCanceledException || !context.CancellationToken.IsCancellationRequested)
        {
            LogSendFailed(logger, e, draft.Sender);
        }

        finally
        {
            // A letter that was refused, or a write that failed: what was taken goes back.
            if (result is not { Outcome: PostSendOutcome.Sent })
            {
                await GiveBackAsync(context, draft, price, account);
            }
        }

        switch (result?.Outcome)
        {
            case PostSendOutcome.Sent:
                break;

            case PostSendOutcome.Self:
                RefuseSend(context, PostMessages.ToYourself);
                return;

            case null:
                RefuseSend(context, PostMessages.Deleted);
                return;

            default:
                RefuseSend(context, PostMessages.NoReceiver);
                return;
        }

        var (_, letter, unread) = result.Value;

        Send(context, new PostSendResponse { Result = 0, Value = PostLetters.Sent(letter!) });

        await network.SendAsync(letter!.ReceiverId!.Value, new PostRecvResponse { PostCount = Count(unread), Value = PostLetters.Received(letter) });

        await FlushAsync(context);
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PostSendBackRequest request)
    {
        var result = await store.SendBackAsync(request.Serial, Me(context), context.CancellationToken);

        var code = result.Outcome switch
        {
            PostReturnOutcome.Returned => 0,
            PostReturnOutcome.FromTheGame => PostMessages.FromTheGame,
            PostReturnOutcome.AlreadyReturned => PostMessages.AlreadyReturned,
            PostReturnOutcome.Full => PostMessages.NoReceiver,
            _ => PostMessages.Deleted
        };

        Send(context, new PostSendBackResponse { Result = code, Serial = request.Serial });

        if (result.Letter is { ReceiverId: { } sender } letter)
        {
            await network.SendAsync(sender, new PostRecvResponse { PostCount = Count(result.Unread), Value = PostLetters.Received(letter) });
        }
    }

    #endregion

    #region Reading and taking

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PostReadRequest request)
    {
        var letter = await store.ReadAsync(request.Serial, Me(context), context.CancellationToken);

        if (letter is null)
        {
            Fail(context, PostOpcode.Read, PostMessages.Deleted);
            return;
        }

        var (mine, _) = await store.CountUnreadAsync(Me(context), Account(context), context.CancellationToken);

        Send(context, new PostReadResponse
        {
            Serial = letter.Id,
            PostFlag = PostLetters.Flag(letter),
            RemainTime = PostLetters.RemainTime(letter),
            PostCount = Count(mine)
        });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PostAccountReadRequest request)
    {
        var letter = await store.ReadAccountAsync(request.Serial, Account(context), context.CancellationToken);

        if (letter is null)
        {
            Fail(context, PostOpcode.AccountRead, PostMessages.Deleted);
            return;
        }

        var (_, account) = await store.CountUnreadAsync(Me(context), Account(context), context.CancellationToken);

        Send(context, new PostAccountReadResponse
        {
            Serial = letter.Id,
            PostFlag = PostLetters.Flag(letter),
            RemainTime = PostLetters.RemainTime(letter),
            PostCount = Count(account)
        });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PostReceiptRequest request)
    {
        var letter = await TakeAsync(context, PostOpcode.Receipt, store.ClaimAsync(request.Serial, Me(context), context.CancellationToken));

        if (letter is not null)
        {
            Send(context, new PostReceiptResponse { Serial = letter.Id, PostFlag = PostLetters.Flag(letter), RemainTime = PostLetters.RemainTime(letter) });

            await FlushAsync(context);
        }
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PostAccountReceiptRequest request)
    {
        var letter = await TakeAsync(context, PostOpcode.AccountReceipt, store.ClaimAccountAsync(request.Serial, Account(context), context.CancellationToken));

        if (letter is not null)
        {
            Send(context, new PostAccountReceiptResponse { Serial = letter.Id, PostFlag = PostLetters.Flag(letter), RemainTime = PostLetters.RemainTime(letter) });

            await FlushAsync(context);
        }
    }

    #endregion

    #region Keeping and deleting

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PostSaveRequest request)
    {
        if (await store.SaveAsync(request.Serial, Me(context), context.CancellationToken) is null)
        {
            Fail(context, PostOpcode.Save, PostMessages.Deleted);
            return;
        }

        Send(context, new PostSaveResponse { Serial = request.Serial });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PostSendDelRequest request)
    {
        if (await store.DeleteSentAsync(request.Serial, Me(context), context.CancellationToken) != PostDeleteOutcome.Deleted)
        {
            Fail(context, PostOpcode.SendDel, PostMessages.Deleted);
            return;
        }

        Send(context, new PostSendDelResponse { Serial = request.Serial });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PostRecvDelRequest request)
    {
        var outcome = await store.DeleteReceivedAsync(request.Serial, Me(context), context.CancellationToken);

        if (outcome != PostDeleteOutcome.Deleted)
        {
            Fail(context, PostOpcode.RecvDel, outcome == PostDeleteOutcome.HasAttachments ? PostMessages.HasAttachments : PostMessages.Deleted);
            return;
        }

        var (mine, _) = await store.CountUnreadAsync(Me(context), Account(context), context.CancellationToken);

        Send(context, new PostRecvDelResponse { Serial = request.Serial, PostCount = Count(mine) });
    }

    public async ValueTask OnHandleAsync(ServiceHandleContext context, PostAccountDelRequest request)
    {
        var outcome = await store.DeleteAccountAsync(request.Serial, Account(context), context.CancellationToken);

        if (outcome != PostDeleteOutcome.Deleted)
        {
            Fail(context, PostOpcode.AccountDel, outcome == PostDeleteOutcome.HasAttachments ? PostMessages.AccountLetterNotDeleted : PostMessages.Deleted);
            return;
        }

        var (_, account) = await store.CountUnreadAsync(Me(context), Account(context), context.CancellationToken);

        Send(context, new PostAccountDelResponse { Serial = request.Serial, PostCount = Count(account) });
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Why a letter is not taken at all, before anything is looked up.
    /// </summary>
    private static int? Refusal(PostSendValue value, string sender)
    {
        if (value.Title.Length > PostPersistent.MaxTitleLength || value.Message.Length > PostPersistent.MaxBodyLength || value.Money < 0 ||
            value.Money > long.MaxValue - PostLetters.Postage)
        {
            return PostMessages.Deleted;
        }

        var name = value.TargetName.Trim();

        if (name.Length == 0 || name.Length > PostPersistent.MaxNameLength)
        {
            return PostMessages.NoReceiver;
        }

        return string.Equals(name, sender, StringComparison.OrdinalIgnoreCase) ? PostMessages.ToYourself : null;
    }

    /// <summary>
    /// Claims what a letter carries and gives it to the player. A claim that cannot be given (no room, too much gold, the
    /// player gone meanwhile) is put back, and the letter keeps it.
    /// </summary>
    private async ValueTask<PostPersistent?> TakeAsync(ServiceHandleContext context, PostOpcode command, Task<PostPersistent?> claim)
    {
        var letter = await claim;

        if (letter is null)
        {
            Fail(context, command, PostMessages.Deleted);
            return null;
        }

        await inventory.EnsureSerialsAsync(letter.Items.Select(e => (e.Item, (int)e.Count)));

        var player = context.Player;

        // Read after the claim: the inventory and the wallet may have changed while it was made.
        var refusal = !world.IsAlive(player) ? 0
            : !inventory.CanReceive(player, ShopCurrency.Gold, letter.Money) ? PostMessages.TooMuchGold
            : !PostLetters.Fits(inventory, player, letter.Items) ? PostMessages.NoRoom
            : (int?)null;

        if (refusal is { } code)
        {
            await store.ReleaseAsync(letter.Id, CancellationToken.None);

            Fail(context, command, code);
            return null;
        }

        if (letter.Money > 0)
        {
            inventory.TryAddGold(player, letter.Money);
        }

        var created = PostLetters.Give(inventory, player, letter.Items);

        if (created.Count > 0)
        {
            Send(context, new ItemCreateResponse { List = created.Select(InventoryService.ToStorageValue).ToArray() });
        }

        return letter;
    }

    /// <summary>
    /// The letter did not go: the sender gets back the gold and the items it took. An inventory that filled up meanwhile
    /// sends the items to the box of the account instead, and a sender who has left meanwhile gets the gold there too, so
    /// nothing is lost.
    /// </summary>
    private async ValueTask GiveBackAsync(ServiceHandleContext context, PostDraft draft, long price, int account)
    {
        var player = context.Player;
        var items = draft.Items;

        await inventory.EnsureSerialsAsync(items.Select(e => (e.Item, (int)e.Count)));

        if (!world.IsAlive(player))
        {
            LogReturnedToAccount(logger, account, price, items.Count);

            await store.SendToAccountAsync(new PostAccountDraft(account, draft.SenderName, draft.Title, draft.Body, price, items), CancellationToken.None);
            return;
        }

        inventory.TryAddGold(player, price);

        if (PostLetters.Fits(inventory, player, items))
        {
            var created = PostLetters.Give(inventory, player, items);

            if (created.Count > 0)
            {
                Send(context, new ItemCreateResponse { List = created.Select(InventoryService.ToStorageValue).ToArray() });
            }

            return;
        }

        await store.SendToAccountAsync(new PostAccountDraft(account, draft.SenderName, draft.Title, draft.Body, 0, items), CancellationToken.None);
    }

    private async ValueTask FlushAsync(ServiceHandleContext context)
    {
        if (world.IsAlive(context.Player))
        {
            await persons.FlushAsync(context.Player);
        }
    }

    private int Me(ServiceHandleContext context) => world.Get<ActorComponent>(context.Player).Identifier;

    private int Account(ServiceHandleContext context) => world.Get<ClaimsComponent>(context.Player).Account;

    private static short Count(int count) => (short)Math.Min(count, short.MaxValue);

    private void Send<T>(ServiceHandleContext context, T message) where T : IResponseHotspotMessage
    {
        if (world.IsAlive(context.Player))
        {
            world.Get<ServerSessionComponent>(context.Player).Send(message);
        }
    }

    private void RefuseSend(ServiceHandleContext context, int code)
    {
        Send(context, new PostSendResponse { Result = code, Value = PostLetters.Empty });
    }

    private void Fail(ServiceHandleContext context, PostOpcode command, int code)
    {
        Send(context, new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.Post, command), (ushort)code));
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "A letter of person {Person} is refused: what it carries could not be written as gone from the sender; it is given back")]
    private static partial void LogNotWritten(ILogger logger, int person);

    [LoggerMessage(Level = LogLevel.Warning, Message = "A letter of person {Person} failed in the database and is refused; what it carried is given back")]
    private static partial void LogSendFailed(ILogger logger, Exception exception, int person);

    [LoggerMessage(Level = LogLevel.Warning, Message = "A letter did not go and its sender has left: {Gold} gold and {Items} items go to the box of account {Account}")]
    private static partial void LogReturnedToAccount(ILogger logger, int account, long gold, int items);

    #endregion
}
