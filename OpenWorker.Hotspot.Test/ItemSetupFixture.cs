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
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A new person of the tables of the client, with the handler of the district for the work on items: set OPENWORKER_TEST_RES
/// to the folder with the *.res files, and OPENWORKER_TEST_SERVER_RES to the server tables for an upgrade or a compose of
/// broaches.
/// </summary>
internal sealed class ItemSetupFixture : IDisposable
{
    public const string TablesVariable = "OPENWORKER_TEST_RES";
    public const string ServerTablesVariable = "OPENWORKER_TEST_SERVER_RES";

    public static ReadOnlyCollection<T> Load<T>(string table, string variable = TablesVariable)
    {
        var folder = Environment.GetEnvironmentVariable(variable);

        if (string.IsNullOrEmpty(folder))
        {
            Assert.Inconclusive($"{variable} is not set.");
        }

        using var reader = new BinaryReader(File.OpenRead(Path.Combine(folder, table + ".res")));

        var count = reader.ReadInt32();

        return new ReadOnlyCollection<T>(Enumerable.Range(0, count).Select(_ => (T)Activator.CreateInstance(typeof(T), reader)!).ToArray());
    }

    /// <summary>
    /// A roll that is always the same, out of ten thousand, after the rolls queued for the next draws.
    /// </summary>
    public sealed class FixedRandom(int roll) : Random
    {
        public int Roll { get; set; } = roll;

        public Queue<int> Queued { get; } = new();

        public override int Next(int minValue, int maxValue) => Queued.TryDequeue(out var next) ? next : Roll;
    }

    public World World { get; } = World.Create();
    public ItemCatalog Catalog { get; }
    public InventoryService Inventory { get; }
    public ReadOnlyCollection<ItemRepairRow> Repairs { get; }
    public FixedRandom Random { get; } = new(1);
    public ItemSetupService Handler { get; }
    public AkashicService Akashic { get; }
    public Entity Player { get; }
    public PersonDirty Dirty { get; } = new(7);

    public ItemSetupFixture(bool withServerTables = false)
    {
        Catalog = new ItemCatalog(
            Load<ItemRow>("tb_Item"),
            Load<ItemClassifyRow>("tb_Item_Classify"),
            Load<InvenSlotExtendRow>("tb_Inven_Slot_Extend"),
            Load<BankSlotExtendRow>("tb_Bank_Slot_Extend"));
        Repairs = Load<ItemRepairRow>("tb_Item_Repair");

        var chances = withServerTables
            ? Load<ReinforceChanceRow>("tb_Reinforce", ServerTablesVariable)
            : new ReadOnlyCollection<ReinforceChanceRow>([]);

        var serials = new LocalItemSerials();
        var answers = new ItemSetupAnswers(World);
        var places = new CostumeBroachPlaces(Catalog, Load<CostumeSocketRow>("tb_Costume_Socket"));

        Inventory = new InventoryService(World, Catalog, serials, broaches: places);
        Handler = new ItemSetupService(
            new ItemRepairService(World, Inventory, Repairs),
            new ItemRestoreService(Inventory, answers),
            new ItemSocketService(Inventory, Load<SocketRow>("tb_Socket"), answers),
            new ItemUpgradeService(Inventory, Load<ReinforceRow>("tb_Reinforce"), chances, answers, Random),
            Akashic = new AkashicService(Inventory, Load<AkashicRecordsRow>("tb_Akashic_Records"), Load<AkashicMakeRow>("tb_Akashic_Make"),
                Load<AkashicDisassembleRow>("tb_Akashic_Disassemble"), answers),
            new ItemBroachService(
                Inventory,
                places,
                ServerTable<BroachComposeRankRow>("TB_BroachCompose_Rank", withServerTables),
                ServerTable<BroachComposeTypeRow>("TB_BroachCompose_Type", withServerTables),
                ServerTable<BroachComposeThemeRow>("TB_BroachCompose_Theme", withServerTables),
                ServerTable<BroachComposeItemRow>("TB_BroachData", withServerTables),
                answers,
                Random));

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

    private static ReadOnlyCollection<T> ServerTable<T>(string table, bool load)
    {
        return load ? Load<T>(table, ServerTablesVariable) : new ReadOnlyCollection<T>([]);
    }

    public ServiceHandleContext Context => new(Player, CancellationToken.None);

    public Inventory Bag => Inventory.InventoryOf(Player)!;

    public ItemInstance Weapon => Bag.At(StorageGroup.AbilityEquip, (int)AbilityEquipSlot.SoulWeapon)!;

    public static StorageSlot SlotOf(ItemInstance item) => new() { Storage = item.Storage, Slot = item.Slot };

    public long Gold
    {
        get => Inventory.Gold(Player);
        set => World.Get<CurrencyComponent>(Player).Gold = value;
    }

    /// <summary>
    /// Gives the person items of the table and returns the stack they went into.
    /// </summary>
    public ItemInstance Give(int item, int count)
    {
        Assert.IsTrue(Inventory.TryGive(Player, item, count), $"The bag takes {count} of {item}");

        return Bag.Items.First(e => e.Item == item);
    }

    public void Dispose()
    {
        World.Dispose();
    }
}
