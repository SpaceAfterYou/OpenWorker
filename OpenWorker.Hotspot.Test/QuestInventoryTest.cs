using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Boosters;
using OpenWorker.Gameplay.Modules.Helpers;
using OpenWorker.Gameplay.Modules.ItemSetup;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Gameplay.Modules.Quests.Components;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Quests.Enums;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The quests against the bags of a person as the servers wire them: the reward granter and the quest items of the inventory,
/// and the items that come into the bags. Needs the item tables of the client (OPENWORKER_TEST_RES).
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class QuestInventoryTest
{
    // Cloth, stacks of 50.
    private const int Material = 710105001;

    private sealed class NoStore : IQuestStore
    {
        public ValueTask<QuestBook?> LoadAsync(int person, CancellationToken cancellationToken) => ValueTask.FromResult<QuestBook?>(null);
    }

    private sealed class Fixture : IDisposable
    {
        public Fixture(QuestCatalog catalog)
        {
            Catalog = new ItemCatalog(
                ItemSetupFixture.Load<ItemRow>("tb_Item"),
                ItemSetupFixture.Load<ItemClassifyRow>("tb_Item_Classify"),
                ItemSetupFixture.Load<InvenSlotExtendRow>("tb_Inven_Slot_Extend"),
                ItemSetupFixture.Load<BankSlotExtendRow>("tb_Bank_Slot_Extend"));

            var serials = new LocalItemSerials();
            var arrivals = new ItemArrivals();

            Inventory = new InventoryService(World, Catalog, serials, arrivals: arrivals);

            // The stores of helpers and boosters are never reached: the person has neither.
            Quests = new QuestManager(
                World,
                new NoStore(),
                catalog,
                new InventoryRewardGranter(Inventory, World),
                new InventoryQuestItems(Inventory),
                new HelperManager(World, null!, HelperCatalog.From([])),
                new BoosterManager(World, null!, null!, TimeProvider.System, NullLogger<BoosterManager>.Instance),
                NullLogger<QuestManager>.Instance);

            // As the servers wire it (AddQuestGameplay).
            arrivals.Listen(Quests.OnItemsArrived);

            Player = World.Create(
                Component.GetComponentType(typeof(ActorComponent)),
                Component.GetComponentType(typeof(InventoryComponent)),
                Component.GetComponentType(typeof(LevelComponent)),
                Component.GetComponentType(typeof(PersonInfoComponent)),
                Component.GetComponentType(typeof(CurrencyComponent)),
                Component.GetComponentType(typeof(QuestComponent)),
                Component.GetComponentType(typeof(DirtyComponent)));

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

            World.Set(Player, new InventoryComponent(new InventoryFactory(Catalog, ItemSetupFixture.Load<CreateClothRow>("tb_Create_Cloth"), serials).CreateNew(person)));
            World.Set(Player, new LevelComponent(1, 0));
            World.Set(Player, new PersonInfoComponent { Name = "Test", Hero = Hero.Haru });
            World.Set(Player, new CurrencyComponent());
            World.Set(Player, default(QuestComponent));
            World.Set(Player, new DirtyComponent(new PersonDirty(7)));
        }

        public World World { get; } = World.Create();
        public ItemCatalog Catalog { get; }
        public InventoryService Inventory { get; }
        public QuestManager Quests { get; }
        public Entity Player { get; }

        public QuestEpisodeState? State(int episode) => World.Get<QuestComponent>(Player).Book!.Find(episode);

        public void Dispose() => World.Dispose();
    }

    private static QuestDefinition Episode(int id, IReadOnlyList<QuestItem> given, IReadOnlyList<QuestItem> taken, params int[] conditions) =>
        new(id, id, QuestContents.Side, 0, 0, [], false, false, given, taken, QuestRewardDefinition.None, conditions);

    [TestMethod]
    public async Task AnEpisodeGivesItsItemsWhenTakenAndTakesThemWhenFinished()
    {
        var bring = new QuestConditionDefinition(5001, QuestConditionType.Collect, QuestConditionTarget.Item, Material, 0, 1, 0, [], []);
        var errand = Episode(50, [new QuestItem(Material, 1)], [new QuestItem(Material, 1)], bring.Id);

        using var f = new Fixture(QuestCatalog.From([errand], [bring]));

        await f.Quests.EnterAsync(f.Player, 7, CancellationToken.None);
        await f.Quests.AcceptAsync(f.Player, errand.Id, CancellationToken.None);

        Assert.AreEqual(1, f.Inventory.CountOf(f.Player, Material), "taking the episode puts its item in the bag");
        Assert.IsTrue(f.State(errand.Id)!.IsDone(0), "the item in the bag counts for the condition that asks for it");

        await f.Quests.CompleteAsync(f.Player, errand.Id, 0, CancellationToken.None);

        Assert.IsTrue(f.Quests.HasFinishedEpisode(f.Player, errand.Id));
        Assert.AreEqual(0, f.Inventory.CountOf(f.Player, Material), "finishing it takes the item back");
    }

    [TestMethod]
    public async Task WhatIsMadeCountsForTheConditionsThatNameIt()
    {
        var recipe = ItemSetupFixture.Load<MakeRow>("tb_Make").First(e => e.Gold > 0 && e.Material1 != 0 && e.Product != e.Material1);

        var make = new QuestConditionDefinition(5101, QuestConditionType.Make, QuestConditionTarget.Item, recipe.Product, 0, 2, 0, [], []);
        var gather = new QuestConditionDefinition(5102, QuestConditionType.Collect, QuestConditionTarget.Item, recipe.Product, 0, 2, 0, [], []);
        var craft = Episode(51, [], [], make.Id, gather.Id);

        using var f = new Fixture(QuestCatalog.From([craft], [make, gather]));

        Assume(f.Catalog.Exists(recipe.Product) && f.Catalog.HomeOf(recipe.Product) == StorageGroup.Common);

        foreach (var (material, count) in recipe.Materials.Where(e => e.Item != 0))
        {
            Assert.IsTrue(f.Inventory.TryGive(f.Player, material, count * 2));
        }

        f.World.Get<CurrencyComponent>(f.Player).Gold = recipe.Gold * 2L;

        await f.Quests.EnterAsync(f.Player, 7, CancellationToken.None);
        await f.Quests.AcceptAsync(f.Player, craft.Id, CancellationToken.None);

        new ItemMakeService(f.Inventory, ItemSetupFixture.Load<MakeRow>("tb_Make"), new ItemSetupAnswers(f.World), f.Quests).Make(f.Player, recipe.Id, 2);

        var state = f.State(craft.Id)!;

        Assert.AreEqual(2, f.Inventory.CountOf(f.Player, recipe.Product));
        Assert.AreEqual((byte)2, state.Steps[0], "making counts every item made");
        Assert.AreEqual((byte)2, state.Steps[1], "and what is made came into the bag");
    }

    private static void Assume(bool condition)
    {
        if (!condition)
        {
            Assert.Inconclusive("The tables have no recipe this test can use.");
        }
    }
}
