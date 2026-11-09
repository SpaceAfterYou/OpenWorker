using OpenWorker.Gameplay.Modules.Npcs;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.ItemSetup.Requests;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A repair at an NPC through the handler of the district, against the tables of the client: set OPENWORKER_TEST_RES to the
/// folder with the *.res files.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class ItemRepairTest
{
    private static ValueTask RepairWeapon(ItemSetupFixture f)
    {
        return f.Handler.OnHandleAsync(
            f.Context,
            new ItemSetupRepairNpcRequest
            {
                Npc = f.NpcOffering(NpcFunction.Shop),
                Target = new StorageSlot { Storage = StorageGroup.AbilityEquip, Slot = (short)AbilityEquipSlot.SoulWeapon }
            });
    }

    [TestMethod]
    public async Task RepairAtAnNpcRestoresTheEnduranceAndChargesTheGoldOfTheTable()
    {
        using var f = new ItemSetupFixture();

        Assert.IsTrue(f.Catalog.TryGet(f.Weapon.Item, out var row));
        Assert.IsTrue(row.Endurance > 50, "The weapon of a new person wears down");

        f.Weapon.Endurance = 50;
        f.Gold = 1_000_000;

        // A weapon: the weapon columns of the rows of its level, of its endurance and of its rank, multiplied.
        var byId = f.Repairs.ToDictionary(e => e.Id);
        var expected = (long)(byId[row.Level].WeaponByLevel * byId[50].WeaponByEndurance * byId[row.Rank].WeaponByRank);

        Assert.IsTrue(expected > 0, "The tables price the repair of the weapon");

        await RepairWeapon(f);

        Assert.AreEqual(row.Endurance, f.Weapon.Endurance);
        Assert.AreEqual(1_000_000 - expected, f.Gold);
        Assert.IsTrue(f.Dirty.Sections.HasFlag(PersonSections.Inventory), "The repaired item is written by the flush");
    }

    [TestMethod]
    public async Task RepairThePersonCannotPayForChangesNothing()
    {
        using var f = new ItemSetupFixture();

        f.Weapon.Endurance = 10;
        f.Gold = 1;

        await RepairWeapon(f);

        Assert.AreEqual(10, f.Weapon.Endurance);
        Assert.AreEqual(1, f.Gold);
    }
}
