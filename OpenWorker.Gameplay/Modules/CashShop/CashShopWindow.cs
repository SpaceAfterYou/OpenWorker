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
/// a client that came from the character selection is given all of it at the entry, before it can open the window. The
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
    ServerContent contents,
    ILogger<CashShopWindow> logger)
{
    private Frame[]? _products;

    /// <summary>
    /// Gives the window its contents at the entry of a client from the character selection, while the cash shop is open.
    /// </summary>
    public ValueTask EnterAsync(Entity player, bool firstConnect, CancellationToken cancellationToken)
    {
        return firstConnect && contents[ServerContentIndex.CashShop] ? LoadAsync(player, cancellationToken) : ValueTask.CompletedTask;
    }

    /// <summary>
    /// The banners and the products, then the balance and the lists of the character as the database has them.
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "The cash shop of person {Person} could not be loaded")]
    private static partial void LogLoadFailed(ILogger logger, Exception exception, int person);
}
