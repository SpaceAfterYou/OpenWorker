using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.DistrictServer.Services;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.ItemSetup;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.ItemSetup.Requests;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A repair at an NPC through the handler of the district, against the tables of the client: set OPENWORKER_TEST_RES to the
/// folder with the *.res files.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class ItemRepairTest
{
    private const string TablesVariable = "OPENWORKER_TEST_RES";

    private static ReadOnlyCollection<T> Load<T>(string table)
    {
        var folder = Environment.GetEnvironmentVariable(TablesVariable);

        if (string.IsNullOrEmpty(folder))
        {
            Assert.Inconclusive($"{TablesVariable} is not set.");
        }

        using var reader = new BinaryReader(File.OpenRead(Path.Combine(folder, table + ".res")));

        var count = reader.ReadInt32();

        return new ReadOnlyCollection<T>(Enumerable.Range(0, count).Select(_ => (T)Activator.CreateInstance(typeof(T), reader)!).ToArray());
    }

    private sealed class Fixture : IDisposable
    {
        public World World { get; } = World.Create();
        public ItemCatalog Catalog { get; }
        public InventoryService Inventory { get; }
        public ReadOnlyCollection<ItemRepairRow> Repairs { get; }
        public ItemSetupService Handler { get; }
        public Entity Player { get; }
        public PersonDirty Dirty { get; } = new(7);

        public Fixture()
        {
            Catalog = new ItemCatalog(
                Load<ItemRow>("tb_Item"),
                Load<ItemClassifyRow>("tb_Item_Classify"),
                Load<InvenSlotExtendRow>("tb_Inven_Slot_Extend"),
                Load<BankSlotExtendRow>("tb_Bank_Slot_Extend"));
            Repairs = Load<ItemRepairRow>("tb_Item_Repair");

            var serials = new LocalItemSerials();

            Inventory = new InventoryService(World, Catalog, serials);
            Handler = new ItemSetupService(new ItemRepairService(World, Inventory, Repairs));

            var factory = new InventoryFactory(Catalog, Load<CreateClothRow>("tb_Create_Cloth"), serials);

            Player = World.Create(
                Component.GetComponentType(typeof(ActorComponent)),
                Component.GetComponentType(typeof(InventoryComponent)),
                Component.GetComponentType(typeof(LevelComponent)),
                Component.GetComponentType(typeof(PersonInfoComponent)),
                Component.GetComponentType(typeof(CurrencyComponent)));

            var person = new PersonPersistent
            {
                Id = 7,
                Name = "Test",
                Hero = Hero.Haru,
                DefaultHairStyle = 0, DefaultHairColor = 0, DefaultEyeColor = 0, DefaultSkinColor = 0,
                EquippedHairStyle = 0, EquippedHairColor = 0, EquippedEyeColor = 0, EquippedSkinColor = 0,
                Location = 0, PositionX = 0, PositionY = 0, PositionZ = 0, RotationX = 0, FatiguePointCommon = 0,
                Account = null!, Gate = null!
            };

            var bag = factory.CreateNew(person);

            bag.Dirty = Dirty;

            World.Set(Player, new InventoryComponent(bag));
            World.Set(Player, new LevelComponent(1, 0));
            World.Set(Player, new PersonInfoComponent { Name = "Test", Hero = Hero.Haru });
            World.Set(Player, new CurrencyComponent());
        }

        public ItemInstance Weapon => Inventory.InventoryOf(Player)!.At(StorageGroup.AbilityEquip, (int)AbilityEquipSlot.SoulWeapon)!;

        public long Gold
        {
            get => Inventory.Gold(Player);
            set => World.Get<CurrencyComponent>(Player).Gold = value;
        }

        public ValueTask RepairWeapon()
        {
            return Handler.OnHandleAsync(
                new ServiceHandleContext(Player, CancellationToken.None),
                new ItemSetupRepairNpcRequest
                {
                    Npc = 1,
                    Target = new StorageSlot { Storage = StorageGroup.AbilityEquip, Slot = (short)AbilityEquipSlot.SoulWeapon }
                });
        }

        public void Dispose()
        {
            World.Dispose();
        }
    }

    [TestMethod]
    public async Task RepairAtAnNpcRestoresTheEnduranceAndChargesTheGoldOfTheTable()
    {
        using var f = new Fixture();

        Assert.IsTrue(f.Catalog.TryGet(f.Weapon.Item, out var row));
        Assert.IsTrue(row.Endurance > 50, "The weapon of a new person wears down");

        f.Weapon.Endurance = 50;
        f.Gold = 1_000_000;

        // A weapon: the weapon columns of the rows of its level, of its endurance and of its rank, multiplied.
        var byId = f.Repairs.ToDictionary(e => e.Id);
        var expected = (long)(byId[row.Level].WeaponByLevel * byId[50].WeaponByEndurance * byId[row.Rank].WeaponByRank);

        Assert.IsTrue(expected > 0, "The tables price the repair of the weapon");

        await f.RepairWeapon();

        Assert.AreEqual(row.Endurance, f.Weapon.Endurance);
        Assert.AreEqual(1_000_000 - expected, f.Gold);
        Assert.IsTrue(f.Dirty.Sections.HasFlag(PersonSections.Inventory), "The repaired item is written by the flush");
    }

    [TestMethod]
    public async Task RepairThePersonCannotPayForChangesNothing()
    {
        using var f = new Fixture();

        f.Weapon.Endurance = 10;
        f.Gold = 1;

        await f.RepairWeapon();

        Assert.AreEqual(10, f.Weapon.Endurance);
        Assert.AreEqual(1, f.Gold);
    }
}
