using System.Collections.ObjectModel;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Res.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The attack of an item is rolled between the least of the client's table and the most of the server's, the same for the same
/// item every time, and the options the upgrade levels give count once the item has reached their level.
/// </summary>
[TestClass]
public sealed class GearRollTest
{
    private const int Weapon = 100;
    private const int Table = 9;
    private const byte AttackEffect = 33;

    private static ReadOnlyCollection<T> Rows<T>(params T[] rows) => new(rows);

    private static StatSheet Worn(GearStats gear, long serial, byte upgrade = 0)
    {
        var inventory = new Inventory(7);

        inventory.Add(StorageGroup.AbilityEquip, 20);
        inventory.Place(StorageGroup.AbilityEquip, 0, new ItemInstance { Serial = serial, Item = Weapon, Count = 1, Upgrade = upgrade });

        var sheet = new StatSheet();

        gear.Collect(inventory, sheet);

        return sheet;
    }

    [TestMethod]
    public void TheAttackOfAnItemIsRolledBetweenItsLeastAndItsMostAndStaysTheSame()
    {
        var catalog = new ItemCatalog(
            Rows(TestRows.Build<ItemRow>(("Id", Weapon), ("Classify", 1), ("Level", (short)1), ("PhysicalAttack", 1000))),
            Rows(TestRows.Build<ItemClassifyRow>(("Id", 1), ("ItemUseType", ItemUseType.ABILITY), ("SlotType", (byte)1))),
            Rows<InvenSlotExtendRow>(),
            Rows<BankSlotExtendRow>());

        var gear = new GearStats(
            catalog,
            rolls: Rows(new ItemRollRow(Weapon, 1000, 1250, 0, 0, Table)),
            reinforceOptions: Rows(new ReinforceOptionRow(Table, [(3, 1, AttackEffect, 40), (6, 1, AttackEffect, 60), (6, 2, AttackEffect, 999)])));

        var seen = new HashSet<float>();

        for (long serial = 1; serial <= 200; serial++)
        {
            var attack = Worn(gear, serial).Added(STAT_TYPE.STAT_PATK_MAX);

            Assert.IsTrue(attack is >= 1000f and <= 1250f, $"{attack} is between the least and the most.");
            Assert.AreEqual(attack, Worn(gear, serial).Added(STAT_TYPE.STAT_PATK_MAX), "The same item rolls the same.");

            seen.Add(attack);
        }

        Assert.IsGreaterThan(100, seen.Count, "The items are spread over the range.");
        Assert.IsTrue(seen.Min() < 1050f && seen.Max() > 1200f, "Both ends of the range are reached.");

        // The options of the upgrade count from the level they begin at (and those of another class do not count as stats).
        var plain = Worn(gear, 7).Added(STAT_TYPE.STAT_PATK_MAX);

        Assert.AreEqual(plain, Worn(gear, 7, upgrade: 2).Added(STAT_TYPE.STAT_PATK_MAX), "Not yet at the first level.");
        Assert.AreEqual(plain + 40f, Worn(gear, 7, upgrade: 3).Added(STAT_TYPE.STAT_PATK_MAX));
        Assert.AreEqual(plain + 100f, Worn(gear, 7, upgrade: 6).Added(STAT_TYPE.STAT_PATK_MAX));
    }
}
