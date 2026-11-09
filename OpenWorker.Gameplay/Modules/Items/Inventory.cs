using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Persistence;

namespace OpenWorker.Gameplay.Modules.Items;

/// <summary>
/// The slots of one storage group of a person.
/// </summary>
public sealed class ItemStorage(StorageGroup group, int size, byte grade)
{
    public StorageGroup Group { get; } = group;

    public ItemInstance?[] Slots { get; private set; } = new ItemInstance?[size];

    /// <summary>
    /// How many times the storage has been extended.
    /// </summary>
    public byte Grade { get; internal set; } = grade;

    public int Size => Slots.Length;

    public int FirstFree()
    {
        return Array.FindIndex(Slots, e => e is null);
    }

    internal void Resize(int size)
    {
        var slots = new ItemInstance?[size];

        Array.Copy(Slots, slots, Math.Min(size, Slots.Length));

        Slots = slots;
    }
}

/// <summary>
/// Everything a person keeps in storages. Changes are noted as they happen and written to the database
/// later, in the order they were made, so the state of the game never waits for it: a change marks
/// <see cref="PersonSections.Inventory"/> on <see cref="Dirty"/>, and the flush takes the items with <see cref="TakeChanges"/>.
/// </summary>
public sealed class Inventory(int person)
{
    private readonly Dictionary<StorageGroup, ItemStorage> _storages = new();
    private readonly HashSet<ItemInstance> _pending = [];
    private readonly List<long> _removed = [];

    public int Person { get; } = person;

    /// <summary>
    /// The mask of the player this inventory belongs to, once it has entered; an inventory of the gate has none.
    /// </summary>
    public PersonDirty? Dirty { get; set; }

    public int[] QuickSlotCards { get; set; } = new int[5];
    public int[] QuickSlotItems { get; set; } = new int[2];

    public IEnumerable<ItemStorage> Storages => _storages.Values;

    public ItemStorage Add(StorageGroup group, int size, byte grade = 0)
    {
        var storage = new ItemStorage(group, size, grade);

        _storages[group] = storage;

        return storage;
    }

    public ItemStorage? Find(StorageGroup group)
    {
        return _storages.GetValueOrDefault(group);
    }

    public ItemInstance? At(StorageGroup group, int slot)
    {
        var storage = Find(group);

        return storage is not null && slot >= 0 && slot < storage.Size ? storage.Slots[slot] : null;
    }

    /// <summary>
    /// Every item in every storage.
    /// </summary>
    public IEnumerable<ItemInstance> Items => _storages.Values.SelectMany(e => e.Slots).OfType<ItemInstance>();

    /// <summary>
    /// Puts the item into an empty slot, which also saves it later.
    /// </summary>
    public void Place(StorageGroup group, int slot, ItemInstance item)
    {
        var storage = _storages[group];

        if (storage.Slots[slot] is not null)
        {
            throw new InvalidOperationException($"Slot {slot} of {group} is taken.");
        }

        storage.Slots[slot] = item;
        item.Storage = group;
        item.Slot = (short)slot;

        _pending.Add(item);
        MarkChanged();
    }

    /// <summary>
    /// Takes the item out of its slot without destroying it, for moving it elsewhere.
    /// </summary>
    public ItemInstance? Lift(StorageGroup group, int slot)
    {
        var item = At(group, slot);

        if (item is null)
        {
            return null;
        }

        _storages[group].Slots[slot] = null;

        return item;
    }

    /// <summary>
    /// Takes the item out of the game for good.
    /// </summary>
    public void Destroy(StorageGroup group, int slot)
    {
        if (Lift(group, slot) is not { } item)
        {
            return;
        }

        _pending.Remove(item);

        if (!item.IsNew)
        {
            _removed.Add(item.Serial);
        }

        MarkChanged();
    }

    /// <summary>
    /// Remembers that the item has changed.
    /// </summary>
    public void Touch(ItemInstance item)
    {
        _pending.Add(item);
        MarkChanged();
    }

    /// <summary>
    /// Something kept with the items changed: the quick slots, how far a storage is extended.
    /// </summary>
    public void MarkChanged()
    {
        Dirty?.Mark(PersonSections.Inventory);
    }

    public void Resize(StorageGroup group, int size, byte grade)
    {
        var storage = _storages[group];

        storage.Resize(size);
        storage.Grade = grade;

        MarkChanged();
    }

    /// <summary>
    /// Takes what has to be written, so the next call only returns what changes from now on.
    /// </summary>
    public PersonChanges? TakeChanges()
    {
        if (_pending.Count == 0 && _removed.Count == 0)
        {
            return null;
        }

        var added = _pending.Where(e => e.IsNew).Select(e => e.ToPersistent(Person)).ToList();
        var changed = _pending.Where(e => !e.IsNew).Select(e => e.ToPersistent(Person)).ToList();

        foreach (var item in _pending)
        {
            item.IsNew = false;
        }

        var changes = new PersonChanges
        {
            Person = Person,
            Added = added,
            Changed = changed,
            Removed = _removed.ToArray()
        };

        _pending.Clear();
        _removed.Clear();

        return changes;
    }

    /// <summary>
    /// Puts back what could not be written.
    /// </summary>
    public void Restore(PersonChanges changes)
    {
        var added = changes.Added.Select(e => e.Id).ToHashSet();
        var items = Items.ToDictionary(e => e.Serial);

        foreach (var serial in changes.Added.Select(e => e.Id).Concat(changes.Changed.Select(e => e.Id)))
        {
            if (items.TryGetValue(serial, out var item))
            {
                item.IsNew = item.IsNew || added.Contains(serial);
                _pending.Add(item);
            }
        }

        _removed.AddRange(changes.Removed);
    }
}
