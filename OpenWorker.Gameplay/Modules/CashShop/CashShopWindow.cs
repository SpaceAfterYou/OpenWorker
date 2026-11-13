using Arch.Core;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.Shop.Responses;
using OpenWorker.Hotspot.Modules.Shop.Types;

namespace OpenWorker.Gameplay.Modules.CashShop;

/// <summary>
/// What the window of the cash shop shows: the banners, the products in parts, the balance and the lists of the character.
/// The client's window reads its first tab as it opens, and has tabs only once the last part of the products has come, so
/// a login is given all of it before the entry of the menu can open the window: at the entry from the character selection,
/// at the next entry into a district when the shop was closed before, and before the notice that the shop has opened. The
/// client keeps it while it plays: a later list is dropped by the window, and the lists of the character replace theirs.
/// </summary>
/// <remarks>
/// The banners and the products are the same for every player: they are encoded once and the same frames are queued to
/// everyone.
/// </remarks>
public sealed partial class CashShopWindow(
    World world,
    CashShopCatalog catalog,
    ICashShopStore store,
    ICashShopLogins logins,
    ServerContent contents,
    ILogger<CashShopWindow> logger)
{
    private Frame[]? _products;

    /// <summary>
    /// Gives the window its contents at an entry into a district while the cash shop is open: always to a client from the
    /// character selection, whose window is new, and to any other only when its login has not been given them yet.
    /// </summary>
    public async ValueTask EnterAsync(Entity player, bool firstConnect, CancellationToken cancellationToken)
    {
        if (!contents[ServerContentIndex.CashShop])
        {
            return;
        }

        if (firstConnect || await LacksAsync(player, cancellationToken))
        {
            await GiveAsync(player, cancellationToken);
        }
    }

    /// <summary>
    /// The cash shop has opened on this server: each player whose login lacks the window's contents is given them, and only
    /// then the notice that enables the entry of the menu, so no client can open the window empty.
    /// </summary>
    public async Task OpenAsync<T>(IReadOnlyCollection<Entity> players, T notice, CancellationToken cancellationToken)
        where T : IResponseHotspotMessage
    {
        await Task.WhenAll(players.ToArray().Select(OpenOneAsync));

        async Task OpenOneAsync(Entity player)
        {
            if (await LacksAsync(player, cancellationToken))
            {
                await GiveAsync(player, cancellationToken);
            }

            if (world.IsAlive(player) && world.TryGet(player, out ServerSessionComponent session))
            {
                session.Send(in notice);
            }
        }
    }

    /// <summary>
    /// Whether the login of the player has not been given the window's contents. When that cannot be told, they are given:
    /// a second time is harmless, an empty window crashes the client.
    /// </summary>
    private async ValueTask<bool> LacksAsync(Entity player, CancellationToken cancellationToken)
    {
        if (!world.IsAlive(player) || !world.TryGet(player, out ClaimsComponent claims))
        {
            return false;
        }

        try
        {
            return !await logins.IsGivenAsync(claims.Key, cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            LogGivenUnknown(logger, e, claims.Key);
            return true;
        }
    }

    private ValueTask GiveAsync(Entity player, CancellationToken cancellationToken)
    {
        return world.IsAlive(player) ? LoadAsync(player, cancellationToken) : ValueTask.CompletedTask;
    }

    /// <summary>
    /// The banners and the products, then the balance and the lists of the character as the database has them; the login
    /// counts as given once all of it is queued.
    /// </summary>
    public async ValueTask LoadAsync(Entity player, CancellationToken cancellationToken)
    {
        var session = world.Get<ServerSessionComponent>(player);

        foreach (var frame in _products ??= Encode())
        {
            session.Send(frame);
        }

        var person = world.Get<ActorComponent>(player).Identifier;
        var account = world.Get<ClaimsComponent>(player).Account;

        try
        {
            var balance = await store.BalanceAsync(account, cancellationToken);
            var sets = await store.ListSetsAsync(person, cancellationToken);

            if (!world.IsAlive(player))
            {
                return;
            }

            session = world.Get<ServerSessionComponent>(player);
            session.Send(new ItemUpdateCashResponse { Total = Balance(balance) });
            session.Send(new ShopCashSetLoadResponse { Sets = sets.Select(Shown).ToList() });

            await logins.GiveAsync(world.Get<ClaimsComponent>(player).Key, cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            LogLoadFailed(logger, e, person);
        }
    }

    /// <summary>
    /// The balance as the client reads it.
    /// </summary>
    public static int Balance(long balance) => (int)Math.Clamp(balance, 0, int.MaxValue);

    public static CashSetValue Shown(CashShopSetPersistent set)
    {
        return new CashSetValue { SetNo = set.SetNo, Name = set.Name, Products = set.Products, Items = set.Items };
    }

    /// <summary>
    /// The banners (none), then the products in parts of the original's size, the last one marked; an empty list is one last
    /// part.
    /// </summary>
    private Frame[] Encode()
    {
        var listing = catalog.Listing;
        var frames = new List<Frame>();

        Add(new ShopBannerLoadResponse { BannerList = [] });

        for (var first = 0; first == 0 || first < listing.Count; first += CashShopCatalog.PartSize)
        {
            Add(new ShopCashLoadResponse
            {
                Last = first + CashShopCatalog.PartSize >= listing.Count,
                ItemList = listing.Skip(first).Take(CashShopCatalog.PartSize).ToList()
            });
        }

        return [.. frames];

        void Add<T>(in T message) where T : IResponseHotspotMessage
        {
            if (Frame.Encode(in message) is { } frame)
            {
                frames.Add(frame);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Whether session {Session} was given the cash shop could not be read; it is given again")]
    private static partial void LogGivenUnknown(ILogger logger, Exception exception, long session);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The cash shop of person {Person} could not be loaded")]
    private static partial void LogLoadFailed(ILogger logger, Exception exception, int person);
}
