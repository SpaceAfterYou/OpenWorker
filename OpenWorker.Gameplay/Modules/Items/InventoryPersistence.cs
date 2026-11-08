using Arch.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Persistent;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Shop.Enums;
using OpenWorker.Persistence;

namespace OpenWorker.Gameplay.Modules.Items;

/// <summary>
/// Loads and saves the items, money, level and experience of the person a connection plays.
/// </summary>
public sealed partial class InventoryPersistence(
    World world,
    IDbContextFactory<PersistenceContext> factory,
    InventoryFactory inventories,
    ILogger<InventoryPersistence> logger)
{
    /// <summary>
    /// Reads the inventory of a person and attaches it to the player. A person without any items yet gets its first ones.
    /// </summary>
    public async ValueTask LoadAsync(Entity player, PersonPersistent person, CancellationToken cancellationToken = default)
    {
        var rows = await factory.LoadItemsAsync(person.Id, cancellationToken);

        var inventory = rows.Count == 0 ? inventories.CreateNew(person) : inventories.Load(person, rows);

        world.Set(player, new InventoryComponent(inventory));
        world.Set(player, new LevelComponent(person.Level, person.Experience));

        var wallet = world.Get<CurrencyComponent>(player);

        wallet[ShopCurrency.Gold] = person.Gold;
        wallet[ShopCurrency.Bp] = person.BattlePoint;
        wallet[ShopCurrency.Ether] = person.Ether;
    }

    /// <summary>
    /// Writes down what has changed since the last time. Writes of one person never overlap.
    /// </summary>
    public async ValueTask FlushAsync(Entity player)
    {
        if (!world.IsAlive(player) || !world.Has<InventoryComponent>(player) || world.Get<InventoryComponent>(player).Value is not { } inventory)
        {
            return;
        }

        await inventory.FlushLock.WaitAsync();

        PersonChanges? changes = null;

        try
        {
            changes = inventory.TakeChanges(() => Snapshot(player, inventory));

            if (changes is not null)
            {
                await factory.SaveChangesAsync(changes);
            }
        }
        catch (Exception exception)
        {
            LogSaveFailed(logger, exception, inventory.Person);

            if (changes is not null)
            {
                inventory.Restore(changes);
            }
        }
        finally
        {
            inventory.FlushLock.Release();
        }
    }

    public async ValueTask FlushAllAsync()
    {
        var players = new List<Entity>();

        world.Query(new QueryDescription().WithAll<InventoryComponent>(), entity => players.Add(entity));

        foreach (var player in players)
        {
            await FlushAsync(player);
        }
    }

    private PersonStateChanges Snapshot(Entity player, Inventory inventory)
    {
        var rank = world.Get<LevelComponent>(player);
        var wallet = world.Get<CurrencyComponent>(player);

        var grades = new byte[(int)StorageGroup.AccountCashStorage + 1];

        foreach (var storage in inventory.Storages)
        {
            if ((int)storage.Group < grades.Length)
            {
                grades[(int)storage.Group] = storage.Grade;
            }
        }

        return new PersonStateChanges(
            rank.Level,
            rank.Experience,
            wallet.Gold,
            wallet.BattlePoint,
            wallet.Ether,
            grades,
            inventory.QuickSlotCards,
            inventory.QuickSlotItems);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Saving the items of person {Person} failed")]
    private static partial void LogSaveFailed(ILogger logger, Exception exception, int person);
}

/// <summary>
/// Saves the items of a person when its connection closes.
/// </summary>
public sealed class InventorySaver(InventoryPersistence persistence) : ISessionCloseHandler
{
    public ValueTask OnSessionClosedAsync(Entity player)
    {
        return persistence.FlushAsync(player);
    }
}

/// <summary>
/// Saves changes of every player now and then, so that a crash of the server loses seconds and not a play session.
/// </summary>
public sealed partial class InventoryFlushService(WorldLoop loop, InventoryPersistence persistence, DatabaseItemSerials serials, ILogger<InventoryFlushService> logger) : BackgroundService
{
    private static TimeSpan Period { get; } = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await serials.PrimeAsync();
        }
        catch (Exception exception)
        {
            LogSerialsNotReserved(logger, exception);
        }

        using var timer = new PeriodicTimer(Period);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await loop.InvokeAsync(() => persistence.FlushAllAsync());
            }
            catch (Exception exception)
            {
                LogFlushFailed(logger, exception);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Item serials could not be reserved yet")]
    private static partial void LogSerialsNotReserved(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Saving items failed")]
    private static partial void LogFlushFailed(ILogger logger, Exception exception);
}
