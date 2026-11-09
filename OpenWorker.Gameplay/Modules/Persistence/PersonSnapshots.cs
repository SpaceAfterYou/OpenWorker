using Arch.Core;
using OpenWorker.Domain.Stores;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Shop.Enums;

namespace OpenWorker.Gameplay.Modules.Persistence;

/// <summary>
/// What one flush takes of one person: the parts that were marked, and their state at that moment.
/// </summary>
public sealed record PersonWrite(Entity Player, PersonDirty Dirty, PersonSections Sections, PersonChanges Changes);

/// <summary>
/// Takes the state of the marked parts of persons into plain records, on the world loop, so that the write can run
/// anywhere: nothing of the world is read after this.
/// </summary>
public sealed class PersonSnapshots(World world, IEnumerable<IPersonDocumentSource> documents, IEnumerable<IPersonPlaceSource> places)
{
    private static QueryDescription Persons { get; } = new QueryDescription().WithAll<DirtyComponent>();

    private readonly IPersonDocumentSource[] _documents = documents.ToArray();
    private readonly IPersonPlaceSource? _place = places.LastOrDefault();

    /// <summary>
    /// Takes what the player changed and clears its mask, or returns null when nothing changed.
    /// </summary>
    public PersonWrite? Take(Entity player)
    {
        if (world.DirtyOf(player) is not { } dirty)
        {
            return null;
        }

        var sections = dirty.Take();

        PersonPlace? place = null;

        if (_place is not null && _place.TryGetPlace(world, player, out var current) && current != dirty.WrittenPlace)
        {
            place = current;
            dirty.WrittenPlace = current;
            sections |= PersonSections.Place;
        }

        if (sections == PersonSections.None)
        {
            return null;
        }

        var inventory = world.TryGet<InventoryComponent>(player, out var bag) ? bag.Value : null;

        var items = (sections & PersonSections.Inventory) != 0 ? inventory?.TakeChanges() : null;
        var state = (sections & PersonSections.Row) != 0 && inventory is not null ? TakeRow(player, inventory) : null;

        var taken = new List<PersonDocument>();

        foreach (var source in _documents)
        {
            if ((sections & source.Section) != 0 && source.Snapshot(world, player) is { } payload)
            {
                taken.Add(new PersonDocument(source.Kind, payload));
            }
        }

        var changes = new PersonChanges
        {
            Person = dirty.Person,
            Added = items?.Added ?? [],
            Changed = items?.Changed ?? [],
            Removed = items?.Removed ?? [],
            State = state,
            Place = place,
            Documents = taken
        };

        return changes.IsEmpty ? null : new PersonWrite(player, dirty, sections, changes);
    }

    /// <summary>
    /// Takes what every player of the server changed.
    /// </summary>
    public List<PersonWrite> TakeAll()
    {
        var players = new List<Entity>();

        world.Query(Persons, entity => players.Add(entity));

        var writes = new List<PersonWrite>();

        foreach (var player in players)
        {
            if (Take(player) is { } write)
            {
                writes.Add(write);
            }
        }

        return writes;
    }

    /// <summary>
    /// Marks again what could not be written, so that the next flush writes it as it is then. A person who has left
    /// in the meantime cannot get it back: false.
    /// </summary>
    public bool Restore(PersonWrite write)
    {
        if (world.DirtyOf(write.Player) != write.Dirty)
        {
            return false;
        }

        write.Dirty.Mark(write.Sections & ~PersonSections.Place);

        if (write.Changes.Place is not null)
        {
            write.Dirty.WrittenPlace = null;
        }

        if (world.TryGet<InventoryComponent>(write.Player, out var bag) && bag.Value is { } inventory)
        {
            inventory.Restore(write.Changes);
        }

        return true;
    }

    private PersonStateChanges TakeRow(Entity player, Inventory inventory)
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

        var currencies = new long[(int)ShopCurrency.Max];

        for (var index = 0; index < currencies.Length; index++)
        {
            currencies[index] = wallet[index];
        }

        return new PersonStateChanges(
            rank.Level,
            rank.Experience,
            wallet.Gold,
            wallet.BattlePoint,
            wallet.Ether,
            currencies,
            grades,
            (int[])inventory.QuickSlotCards.Clone(),
            (int[])inventory.QuickSlotItems.Clone());
    }
}
