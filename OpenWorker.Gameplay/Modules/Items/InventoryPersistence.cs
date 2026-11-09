using Arch.Core;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Hotspot.Modules.Shop.Enums;

namespace OpenWorker.Gameplay.Modules.Items;

/// <summary>
/// Loads the items, the akashic record book, the wallet, level and experience of the person a connection plays, and starts
/// the mask the flush writes them by (<see cref="DirtyComponent"/>). Without the documents of the person (the gate) the book
/// starts empty.
/// </summary>
public sealed partial class InventoryPersistence(
    World world,
    IPersonStore store,
    InventoryFactory inventories,
    ILogger<InventoryPersistence> logger,
    PersonStateStore? documents = null)
{
    /// <summary>
    /// Reads the inventory of a person and attaches it to the player. A person without any items yet gets its first ones,
    /// which the next flush writes.
    /// </summary>
    public async ValueTask LoadAsync(Entity player, PersonPersistent person, CancellationToken cancellationToken = default)
    {
        var rows = await store.LoadItemsAsync(person.Id, cancellationToken);

        var records = documents is null
            ? null
            : await documents.LoadAsync<AkashicRecordBook>(person.Id, PersonStateKind.AkashicRecords, cancellationToken);

        // The player may have left while the items were read.
        if (!world.IsAlive(player))
        {
            return;
        }

        var dirty = new PersonDirty(person.Id);

        var inventory = rows.Count == 0 ? inventories.CreateNew(person) : inventories.Load(person, rows);

        inventory.Dirty = dirty;
        inventory.AkashicRecords = records ?? new AkashicRecordBook();

        if (rows.Count == 0)
        {
            dirty.Mark(PersonSections.Inventory);
        }

        world.Set(player, new InventoryComponent(inventory));
        world.Set(player, new LevelComponent(person.Level, person.Experience));
        world.Set(player, LoadWallet(person));
        world.Set(player, new DirtyComponent(dirty));
    }

    /// <summary>
    /// Writes the items of a person now, for a server that has no flush: the gate saves the first items of a character
    /// it has just created, so that the district finds them.
    /// </summary>
    public async ValueTask SaveItemsAsync(Entity player)
    {
        if (!world.IsAlive(player) || !world.Has<InventoryComponent>(player) || world.Get<InventoryComponent>(player).Value is not { } inventory)
        {
            return;
        }

        if (inventory.TakeChanges() is not { } changes)
        {
            return;
        }

        try
        {
            await store.SaveChangesAsync(changes);
        }

        catch (Exception exception)
        {
            LogSaveFailed(logger, exception, inventory.Person);

            inventory.Restore(changes);
        }
    }

    /// <summary>
    /// A wallet of its own for every player (the default of the component shares one array between all of them), filled
    /// from the whole wallet and then from the columns of gold, battle points and ether, which older rows have alone.
    /// </summary>
    private static CurrencyComponent LoadWallet(PersonPersistent person)
    {
        var wallet = new CurrencyComponent();

        for (var index = 0; index < person.Currencies.Length && index < (int)ShopCurrency.Max; index++)
        {
            wallet[index] = person.Currencies[index];
        }

        wallet[ShopCurrency.Gold] = person.Gold;
        wallet[ShopCurrency.Bp] = person.BattlePoint;
        wallet[ShopCurrency.Ether] = person.Ether;

        return wallet;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Saving the items of person {Person} failed")]
    private static partial void LogSaveFailed(ILogger logger, Exception exception, int person);
}
