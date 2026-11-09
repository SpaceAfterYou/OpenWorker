using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Persons.Responses;

namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// What the gear of a person adds to its stats: the attack and defense written in the item table and the options of the item
/// that name a stat. Options that start a skill or a buff are not stats and are left out.
/// </summary>
public static class EquipmentStats
{
    private const int StatCount = (int)STAT_TYPE.MAX_STAT;

    public static StatBonuses Calculate(Inventory? inventory, ItemCatalog catalog, Hero hero)
    {
        var sum = new float[StatCount];

        foreach (var item in inventory?.Find(StorageGroup.AbilityEquip)?.Slots.OfType<ItemInstance>() ?? [])
        {
            if (!catalog.TryGet(item.Item, out var row))
            {
                continue;
            }

            sum[(int)STAT_TYPE.STAT_PATK_MAX] += row.PhysicalAttack;
            sum[(int)STAT_TYPE.STAT_MATK_MAX] += row.MagicalAttack;
            sum[(int)STAT_TYPE.STAT_PDEF] += row.PhysicalDefense;
            sum[(int)STAT_TYPE.STAT_MDEF] += row.MagicalDefense;

            (byte Class, short Type, int Value)[] statics =
            [
                (row.StatClass1, row.StatType1, row.StatValue1),
                (row.StatClass2, row.StatType2, row.StatValue2),
                (row.StatClass3, row.StatType3, row.StatValue3),
                (row.StatClass4, row.StatType4, row.StatValue4),
                (row.StatClass5, row.StatType5, row.StatValue5)
            ];

            foreach (var (restriction, type, value) in statics)
            {
                if (restriction == 0 || restriction == (byte)hero)
                {
                    Add(sum, type, value);
                }
            }

            foreach (var option in item.Options)
            {
                Add(sum, option.Type, option.Option);
            }
        }

        return new StatBonuses
        {
            Health = sum[(int)STAT_TYPE.STAT_HP_MAX],
            PhysicalAttack = sum[(int)STAT_TYPE.STAT_PATK_MAX],
            MagicalAttack = sum[(int)STAT_TYPE.STAT_MATK_MAX],
            PhysicalDefense = sum[(int)STAT_TYPE.STAT_PDEF],
            MagicalDefense = sum[(int)STAT_TYPE.STAT_MDEF],
            PhysicalAccuracy = sum[(int)STAT_TYPE.STAT_PAR],
            MagicalAccuracy = sum[(int)STAT_TYPE.STAT_MAR],
            PhysicalEvasion = sum[(int)STAT_TYPE.STAT_PARP],
            MagicalEvasion = sum[(int)STAT_TYPE.STAT_MARP],
            GrazeDamage = sum[(int)STAT_TYPE.STAT_ADR],
            PhysicalCriticalChance = sum[(int)STAT_TYPE.STAT_PCP],
            MagicalCriticalChance = sum[(int)STAT_TYPE.STAT_MCP],
            PhysicalCriticalResist = sum[(int)STAT_TYPE.STAT_PCRP],
            MagicalCriticalResist = sum[(int)STAT_TYPE.STAT_MCRP],
            PhysicalCriticalDamage = sum[(int)STAT_TYPE.STAT_PCA],
            PhysicalDamageReduction = sum[(int)STAT_TYPE.STAT_PDSR],
            MagicalDamageReduction = sum[(int)STAT_TYPE.STAT_MDSR]
        };
    }

    private static void Add(float[] sum, int type, int value)
    {
        if (type > 0 && type < StatCount)
        {
            sum[type] += value;
        }
    }
}
