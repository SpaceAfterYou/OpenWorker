using System.Collections.ObjectModel;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Items;

/// <summary>
/// Builds the inventory of a person from what was saved, or from nothing for a person that has no items yet.
/// </summary>
public sealed class InventoryFactory(
    ItemCatalog catalog, ReadOnlyCollection<CreateClothRow> outfits, IItemSerials serials, CostumeBroachPlaces? broaches = null)
{
    private readonly RowIndex<int, CreateClothRow> _outfits = outfits.IndexBy(e => (int)e.Id);

    /// <summary>
    /// The inventory a person had when it was last saved.
    /// </summary>
    public Inventory Load(PersonPersistent person, IReadOnlyCollection<ItemPersistent> rows)
    {
        var inventory = Empty(person);

        foreach (var row in rows.OrderBy(e => e.Id))
        {
            var group = (StorageGroup)row.Storage;

            // What a helper wears lies in a storage of its own, and a place that is taken keeps the older item.
            if (row.Storage == ItemPersistent.HelperStorage)
            {
                inventory.LoadOnHelper(row.Slot, ItemInstance.From(row));
                continue;
            }

            var storage = inventory.Find(group);

            if (storage is null || row.Slot < 0 || row.Slot >= storage.Size || storage.Slots[row.Slot] is not null)
            {
                continue;
            }

            var item = ItemInstance.From(row);

            // Clothing saved before it was given its places for broaches gets them from its table.
            if (broaches is not null && item.BroachState == ItemInstance.NoBroaches)
            {
                item.BroachState = broaches.StateOf(item.Item);
            }

            storage.Slots[row.Slot] = item;
        }

        return inventory;
    }

    /// <summary>
    /// The most items a character is born with: one weapon and five pieces of clothes.
    /// </summary>
    public const int MostStartingItems = 6;

    /// <summary>
    /// Waits until the serials of the first items of that many persons are ready, so that <see cref="CreateNew"/>, which
    /// cannot wait, finds them. After the wait the caller checks the player again.
    /// </summary>
    public ValueTask EnsureSerialsAsync(int persons = 1)
    {
        return serials.EnsureAsync(persons * MostStartingItems);
    }

    /// <summary>
    /// What a character is born with: the weapon of its class and the clothes it was made with.
    /// </summary>
    public Inventory CreateNew(PersonPersistent person)
    {
        var inventory = Empty(person);

        var weapon = new ItemInstance
        {
            Serial = serials.NextSerial(),
            Item = WeaponOf(person.Hero),
            Count = 1,
            BindState = ItemBinding.StartingItem
        };

        if (catalog.Exists(weapon.Item))
        {
            inventory.Place(StorageGroup.AbilityEquip, (int)AbilityEquipSlot.SoulWeapon, weapon);
        }

        var outfit = _outfits.GetOrDefault(person.Outfit);

        if (outfit.Id != 0)
        {
            foreach (var id in (int[])[outfit.Item1, outfit.Item2, outfit.Item3, outfit.Item4, outfit.Item5])
            {
                if (catalog.EquipPlaceOf(id) is { Storage: StorageGroup.ShapeEquip } place &&
                    inventory.At(StorageGroup.ShapeEquip, place.Slots[0]) is null)
                {
                    inventory.Place(StorageGroup.ShapeEquip, place.Slots[0], new ItemInstance
                    {
                        Serial = serials.NextSerial(),
                        Item = id,
                        Count = 1,
                        BindState = ItemBinding.StartingItem
                    });
                }
            }
        }

        return inventory;
    }

    /// <summary>
    /// The class weapon every character starts with.
    /// </summary>
    public static int WeaponOf(Hero hero)
    {
        return (int)hero is >= 1 and <= 6 ? 110000001 + (int)hero * 1000000 : 0;
    }

    private Inventory Empty(PersonPersistent person)
    {
        var inventory = new Inventory(person.Id)
        {
            QuickSlotCards = Pad(person.QuickSlotCards, 5),
            QuickSlotItems = Pad(person.QuickSlotItems, 2)
        };

        foreach (var group in ItemCatalog.PersonalStorages)
        {
            var grade = (int)group < person.StorageGrades.Length ? person.StorageGrades[(int)group] : (byte)0;
            var size = ItemCatalog.IsEquipment(group) ? ItemCatalog.EquipmentSize(group) : catalog.SizeAt(group, grade);

            inventory.Add(group, size, grade);
        }

        return inventory;
    }

    private static int[] Pad(int[] values, int length)
    {
        return Enumerable.Range(0, length).Select(i => i < values.Length ? values[i] : 0).ToArray();
    }
}
