using System.Collections.ObjectModel;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Res.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Items;

/// <summary>
/// Where an item may be worn: the storage and the slots of it, in the order they are tried.
/// </summary>
public readonly record struct EquipPlace(StorageGroup Storage, short[] Slots);

/// <summary>
/// What it takes and gives to extend a storage by one step.
/// </summary>
public readonly record struct StorageExtension(byte NeedLevel, SlotExtendPaymentType Payment, int Value, int TotalSlots);

/// <summary>
/// The rules of items taken from the tables: where an item lives, how many fit in a slot, who may wear it.
/// </summary>
public sealed class ItemCatalog
{
    // The slot type of the item table, as it is called in the client.
    private const int SoulWeapon = 1;
    private const int SubWeapon = 2;
    private const int FirstMemory = 101;
    private const int LastMemory = 110;
    private const int Pendant = 111;
    private const int Earring = 131;
    private const int Ring = 141;
    private const int HeadGear = 151;
    private const int ShoulderGear = 161;
    private const int BodyGear = 171;
    private const int LegGear = 181;
    private const int FirstShape = 0x0B;

    private readonly Dictionary<int, ItemRow> _items;
    private readonly Dictionary<int, ItemClassifyRow> _classify;
    private readonly ReadOnlyCollection<InvenSlotExtendRow> _inventory;
    private readonly ReadOnlyCollection<BankSlotExtendRow> _bank;

    public ItemCatalog(
        ReadOnlyCollection<ItemRow> items,
        ReadOnlyCollection<ItemClassifyRow> classify,
        ReadOnlyCollection<InvenSlotExtendRow> inventory,
        ReadOnlyCollection<BankSlotExtendRow> bank)
    {
        _items = items.GroupBy(e => e.Id).ToDictionary(e => e.Key, e => e.First());
        _classify = classify.GroupBy(e => e.Id).ToDictionary(e => e.Key, e => e.First());
        _inventory = inventory;
        _bank = bank;
    }

    public bool Exists(int item)
    {
        return _items.ContainsKey(item);
    }

    public bool TryGet(int item, out ItemRow row)
    {
        return _items.TryGetValue(item, out row);
    }

    public ItemClassifyRow? Classify(int item)
    {
        return _items.TryGetValue(item, out var row) && _classify.TryGetValue(row.Classify, out var classify)
            ? classify
            : null;
    }

    /// <summary>
    /// How many of an item fit in a slot.
    /// </summary>
    public short StackLimit(int item)
    {
        return _items.TryGetValue(item, out var row) ? Math.Max((short)1, row.Stack) : (short)1;
    }

    /// <summary>
    /// The storage an item is put into when it is picked up or bought.
    /// </summary>
    public StorageGroup HomeOf(int item)
    {
        return Classify(item)?.Field10 switch
        {
            2 => StorageGroup.Costume,
            9 => StorageGroup.Cube,
            13 => StorageGroup.Cash,
            _ => StorageGroup.Common
        };
    }

    /// <summary>
    /// The storage group an item of this kind may be kept in, the inventory and its bank are of one kind.
    /// </summary>
    public static StorageGroup KindOf(StorageGroup group)
    {
        return group switch
        {
            StorageGroup.CommonStorage => StorageGroup.Common,
            StorageGroup.CostumeStorage => StorageGroup.Costume,
            StorageGroup.CashStorage => StorageGroup.Cash,
            _ => group
        };
    }

    public static bool IsEquipment(StorageGroup group)
    {
        return group is StorageGroup.ShapeEquip or StorageGroup.AbilityEquip;
    }

    public EquipPlace? EquipPlaceOf(int item)
    {
        if (Classify(item) is not { } classify)
        {
            return null;
        }

        var type = (int)classify.Field11;

        switch (classify.ItemUseType)
        {
            case ItemUseType.SHAPE:
            {
                var slot = type - FirstShape;

                return slot is >= 0 and < ItemModuleDefines.EquippedItemCount
                    ? new EquipPlace(StorageGroup.ShapeEquip, [(short)slot])
                    : null;
            }

            case ItemUseType.ABILITY:
            {
                return type switch
                {
                    SoulWeapon => new EquipPlace(StorageGroup.AbilityEquip, [(short)AbilityEquipSlot.SoulWeapon]),
                    SubWeapon => new EquipPlace(StorageGroup.AbilityEquip, [(short)AbilityEquipSlot.SubWeapon]),
                    Pendant => new EquipPlace(StorageGroup.AbilityEquip, [(short)AbilityEquipSlot.Pendant]),
                    Earring => new EquipPlace(StorageGroup.AbilityEquip, [(short)AbilityEquipSlot.Earring]),
                    Ring => new EquipPlace(StorageGroup.AbilityEquip, [(short)AbilityEquipSlot.Ring1, (short)AbilityEquipSlot.Ring2]),
                    HeadGear => new EquipPlace(StorageGroup.AbilityEquip, [(short)AbilityEquipSlot.HeadGear]),
                    ShoulderGear => new EquipPlace(StorageGroup.AbilityEquip, [(short)AbilityEquipSlot.ShoulderGear]),
                    BodyGear => new EquipPlace(StorageGroup.AbilityEquip, [(short)AbilityEquipSlot.BodyGear]),
                    LegGear => new EquipPlace(StorageGroup.AbilityEquip, [(short)AbilityEquipSlot.LegGear]),
                    >= FirstMemory and <= LastMemory => new EquipPlace(StorageGroup.AbilityEquip, [(short)((int)AbilityEquipSlot.Memory1 + type - FirstMemory)]),
                    _ => null
                };
            }

            default:
            {
                return null;
            }
        }
    }

    /// <summary>
    /// The level and the class an item asks for. A class of 0 is for everyone.
    /// </summary>
    public (short Level, byte Class) Limits(int item)
    {
        return _items.TryGetValue(item, out var row) ? (row.Field12, row.Field13) : ((short)0, (byte)0);
    }

    /// <summary>
    /// The size of a storage before it is extended: one step back from the first row of its table.
    /// </summary>
    public int InitialSize(StorageGroup group)
    {
        var first = Extension(group, 1);
        var second = Extension(group, 2);

        if (first is null)
        {
            return group switch
            {
                StorageGroup.Common or StorageGroup.CommonStorage => 24,
                StorageGroup.Costume or StorageGroup.CostumeStorage => 48,
                _ => 48
            };
        }

        return first.Value.TotalSlots - (second is null ? 12 : second.Value.TotalSlots - first.Value.TotalSlots);
    }

    public int SizeAt(StorageGroup group, int grade)
    {
        return grade <= 0 ? InitialSize(group) : Extension(group, grade)?.TotalSlots ?? InitialSize(group);
    }

    public StorageExtension? Extension(StorageGroup group, int step)
    {
        switch (group)
        {
            case StorageGroup.Common:
            case StorageGroup.Costume:
            case StorageGroup.Cash:
            case StorageGroup.Cube:
            {
                foreach (var row in _inventory)
                {
                    if (row.Field1 != step)
                    {
                        continue;
                    }

                    return group switch
                    {
                        StorageGroup.Common => Create(row.Field2, row.Field3, row.Field4, row.Field6),
                        StorageGroup.Costume => Create(row.Field7, row.Field8, row.Field9, row.Field11),
                        StorageGroup.Cash => Create(row.Field12, row.Field13, row.Field14, row.Field16),
                        _ => Create(row.Field17, row.Field18, row.Field19, row.Field21)
                    };
                }

                return null;
            }

            case StorageGroup.CommonStorage:
            case StorageGroup.CostumeStorage:
            {
                foreach (var row in _bank)
                {
                    if (row.Field1 != step)
                    {
                        continue;
                    }

                    return group == StorageGroup.CommonStorage
                        ? Create(row.Field2, row.Field3, row.Field4, row.Field6)
                        : Create(row.Field7, row.Field8, row.Field9, row.Field11);
                }

                return null;
            }

            default:
            {
                return null;
            }
        }

        static StorageExtension? Create(int level, int payment, int value, int total)
        {
            return total > 0 ? new StorageExtension((byte)level, (SlotExtendPaymentType)payment, value, total) : null;
        }
    }

    /// <summary>
    /// The storages every person has and how big they start.
    /// </summary>
    public static readonly StorageGroup[] PersonalStorages =
    [
        StorageGroup.ShapeEquip,
        StorageGroup.AbilityEquip,
        StorageGroup.Common,
        StorageGroup.Costume,
        StorageGroup.Cash,
        StorageGroup.Cube,
        StorageGroup.CommonStorage,
        StorageGroup.CostumeStorage
    ];

    public int EquipmentSize(StorageGroup group)
    {
        return group == StorageGroup.ShapeEquip ? ItemModuleDefines.EquippedItemCount : (int)AbilityEquipSlot.Max;
    }
}
