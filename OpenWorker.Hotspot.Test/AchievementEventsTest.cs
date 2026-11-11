using System.Collections.ObjectModel;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Modules.DailyMissions;
using OpenWorker.Gameplay.Modules.DailyMissions.Components;
using OpenWorker.Gameplay.Modules.Drops;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.ItemSetup;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Persons;
using OpenWorker.Gameplay.Modules.Progress;
using OpenWorker.Gameplay.Modules.Progress.Components;
using OpenWorker.Gameplay.Modules.Rewards;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Hotspot.Modules.DailyMissions.Enums;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Shop.Enums;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// One chain of achievements for each kind of event (and target) a test counts, each too long for a test to reach its end,
/// and the person who has started them all.
/// </summary>
internal sealed class AchievementChains
{
    /// <summary>
    /// The first achievement of the first chain; the next chain starts ten after it.
    /// </summary>
    public const int FirstAchievement = 5_000_000;

    private const long TooMany = 1_000_000;

    private readonly World _world;

    public AchievementChains(World world, params (AchievementType Type, int Target)[] chains)
        : this(world, TooMany, null, chains)
    {
    }

    /// <param name="world">The world of the person.</param>
    /// <param name="count">How many events end each chain.</param>
    /// <param name="openTitles">The titles the achievements reached open.</param>
    /// <param name="chains">The kind and the target of each chain.</param>
    public AchievementChains(World world, long count, OpenTitleManager? openTitles, params (AchievementType Type, int Target)[] chains)
    {
        _world = world;

        var rules = chains
            .Select((e, i) => new AchievementRule(FirstAchievement + (i * 10), 1, i + 1, (ushort)e.Type, e.Target, count, 0, 0, 0, [], []))
            .ToArray();

        Catalog = AchievementCatalog.Of(rules, rules.Select(e => new AchievementBegin(e.Id, 1)));
        Manager = new AchievementManager(
            world, new PersonStateStore(null!), Catalog, null!, null!, new ReadOnlyCollection<MonsterRow>([]), NullLogger<AchievementManager>.Instance, Time, openTitles);
    }

    public FakeTimeProvider Time { get; } = new();

    public AchievementCatalog Catalog { get; }

    public AchievementManager Manager { get; }

    /// <summary>
    /// The person has the chains started and is at the clock of the manager.
    /// </summary>
    public void Enter(Entity player)
    {
        var book = new AchievementBook();

        book.Open(Catalog, 1);

        var component = new AchievementComponent { Person = 7, Book = book, PlayCountedAt = Time.GetTimestamp() };

        if (_world.Has<AchievementComponent>(player))
        {
            _world.Set(player, component);
        }
        else
        {
            _world.Add(player, component);
        }
    }

    public long Counted(Entity player, AchievementType type, int target = 0)
    {
        return _world.Get<AchievementComponent>(player).Book!.Find((ushort)type, target)!.Count;
    }
}

/// <summary>
/// What a person does counts for the chains of achievements of its kind: the services tell the manager, the manager counts
/// by kind and target, and the minutes of play are counted whole on the tick. The tests that read item tables need
/// OPENWORKER_TEST_RES (and OPENWORKER_TEST_SERVER_RES for an upgrade and for taking items apart).
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class AchievementEventsTest
{
    private const int Maze = 21011;

    /// <summary>
    /// Every roll gives its lowest value.
    /// </summary>
    private sealed class LowestRandom : Random
    {
        public override int Next(int minValue, int maxValue) => minValue;
    }

    /// <summary>
    /// A reward granter that gives every reward.
    /// </summary>
    private sealed class Generous : IRewardGranter
    {
        public ValueTask<bool> TryGrantAsync(Entity player, Reward reward, RewardSource source, CancellationToken cancellationToken) => ValueTask.FromResult(true);
    }

    private static AbilityComponent Ability(int health)
    {
        var life = new AbilityComponentEntry { Current = health, Max = 100 };

        return new AbilityComponent { Health = life, SoulGain = life, SoulVapor = life, Stamina = life, SuperArmor = life, Speed = new SpeedComponentEntry { Move = 1f, Attack = 1f } };
    }

    [TestMethod]
    public void AnEventCountsForTheChainOfItsKindAndTargetAndTheComboKeepsTheHighest()
    {
        using var world = World.Create();
        var chains = new AchievementChains(world, (AchievementType.NpcCredit, 1001), (AchievementType.NpcCredit, 1002), (AchievementType.Combo, 0));
        var player = world.Create(Component.GetComponentType(typeof(AchievementComponent)));

        chains.Enter(player);

        chains.Manager.Count(player, AchievementType.NpcCredit, 5, target: 1001);
        chains.Manager.Count(player, AchievementType.NpcCredit, 2, target: 1001);
        chains.Manager.Count(player, AchievementType.NpcCredit, 3, target: 1003);
        chains.Manager.Count(player, AchievementType.Death);
        chains.Manager.Count(player, AchievementType.Combo, 40);
        chains.Manager.Count(player, AchievementType.Combo, 25);

        Assert.AreEqual(7, chains.Counted(player, AchievementType.NpcCredit, 1001), "the points of a group add up");
        Assert.AreEqual(0, chains.Counted(player, AchievementType.NpcCredit, 1002), "another group is another chain, and a group with no chain counts nowhere");
        Assert.AreEqual(40, chains.Counted(player, AchievementType.Combo), "a shorter combo does not lower the longest");
    }

    [TestMethod]
    public async Task TheMinutesAPersonHasPlayedAreCountedWholeOnTheTick()
    {
        using var world = World.Create();
        var chains = new AchievementChains(world, (AchievementType.PlayTime, 0));
        var player = world.Create(Component.GetComponentType(typeof(AchievementComponent)));

        chains.Enter(player);

        chains.Time.Advance(TimeSpan.FromSeconds(90));
        await chains.Manager.TickAsync(CancellationToken.None);

        Assert.AreEqual(1, chains.Counted(player, AchievementType.PlayTime), "a minute and a half is a minute");

        chains.Time.Advance(TimeSpan.FromSeconds(30));
        await chains.Manager.TickAsync(CancellationToken.None);

        Assert.AreEqual(2, chains.Counted(player, AchievementType.PlayTime), "the half minute left over is not lost");

        chains.Time.Advance(TimeSpan.FromSeconds(59));
        await chains.Manager.TickAsync(CancellationToken.None);

        Assert.AreEqual(2, chains.Counted(player, AchievementType.PlayTime), "less than a minute counts for nothing yet");
    }

    [TestMethod]
    public void BattlePointsAddedToThePurseCountWhateverTheirSourceAndSpendingThemDoesNot()
    {
        using var world = World.Create();
        var chains = new AchievementChains(world, (AchievementType.BattlePointsGained, 0));
        var arrivals = new ItemArrivals();
        var inventory = new InventoryService(
            world,
            new ItemCatalog(new ReadOnlyCollection<ItemRow>([]), new ReadOnlyCollection<ItemClassifyRow>([]), new ReadOnlyCollection<InvenSlotExtendRow>([]), new ReadOnlyCollection<BankSlotExtendRow>([])),
            new LocalItemSerials(),
            arrivals: arrivals);

        arrivals.ListenBattlePoints(chains.Manager.OnBattlePointsGained);

        var player = world.Create(
            Component.GetComponentType(typeof(ActorComponent)),
            Component.GetComponentType(typeof(CurrencyComponent)),
            Component.GetComponentType(typeof(AchievementComponent)));

        world.Set(player, new ActorComponent(7, ActorType.User));
        world.Set(player, new CurrencyComponent());
        chains.Enter(player);

        Assert.IsTrue(inventory.TryAddCurrency(player, ShopCurrency.Bp, 500));
        Assert.IsTrue(inventory.TryAddCurrency(player, ShopCurrency.Bp, -200));
        Assert.IsTrue(inventory.TryAddCurrency(player, ShopCurrency.Gold, 900));

        Assert.AreEqual(500, chains.Counted(player, AchievementType.BattlePointsGained));
    }

    [TestMethod]
    public void EveryMonsterAnAttackHitsCountsAsASkillUsedTheComboCountsAndADeathIsOneOnlyAtNoLifeLeft()
    {
        using var world = World.Create();
        var chains = new AchievementChains(world, (AchievementType.SkillUsed, 0), (AchievementType.Combo, 0), (AchievementType.Death, 0));
        var results = new MazeResults(world, achievements: chains.Manager);
        var player = world.Create(Component.GetComponentType(typeof(AchievementComponent)), Component.GetComponentType(typeof(AbilityComponent)));

        world.Set(player, Ability(100));
        chains.Enter(player);
        results.Start(player, Maze);

        // The first hit starts a combo of its targets less one; the next goes on from it.
        results.OnMonstersHit(player, 2, 50);
        results.OnMonstersHit(player, 3, 50);

        Assert.AreEqual(5, chains.Counted(player, AchievementType.SkillUsed), "two monsters and three monsters");
        Assert.AreEqual(4, chains.Counted(player, AchievementType.Combo));

        world.Set(player, Ability(40));
        results.OnHealthChanged(player);

        Assert.AreEqual(0, chains.Counted(player, AchievementType.Death), "a hit that leaves life is not a death");

        world.Set(player, Ability(0));
        results.OnHealthChanged(player);

        Assert.AreEqual(1, chains.Counted(player, AchievementType.Death));
    }

    [TestMethod]
    public async Task AFinishedDailyMissionCountsOnce()
    {
        using var world = World.Create();
        var chains = new AchievementChains(world, (AchievementType.MissionDone, 0));
        const int item = 800000999;
        var catalog = DailyMissionCatalog.From(
        [
            new DailyMissionDefinition(
                1, DailyMissionType.Special, 10003, DailyMissionFinish.Collect, DailyMissionTarget.Item, item, 0, 0, 0, 0, 0, 2,
                DailyMissionDates.Parse("1000-01-01 00:00:00", "1000-01-01 23:00:00"), [new RewardItem(item, 1)])
        ]);
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 7, 12, 0, 0, TimeSpan.Zero));
        var missions = new DailyMissionManager(
            world, new PersonStateStore(null!), catalog, new Generous(), time, Options.Create(new DailyMissionOptions()), NullLogger<DailyMissionManager>.Instance, chains.Manager);

        var player = world.Create(
            Component.GetComponentType(typeof(AchievementComponent)),
            Component.GetComponentType(typeof(DailyMissionComponent)),
            Component.GetComponentType(typeof(ServerSessionComponent)));
        var now = time.GetLocalNow();

        world.Set(player, new ServerSessionComponent(new RecordingConnection()));

        world.Set(player, new DailyMissionComponent
        {
            Person = 7,
            Book = new DailyMissionBook
            {
                Missions = [new DailyMissionEntry { Mission = 1, State = DailyMissionState.Accept, Accept = now, Start = now.AddHours(-1), End = now.AddDays(1) }]
            }
        });
        chains.Enter(player);

        await missions.OnItemCollectedAsync(player, item, 1, CancellationToken.None);

        Assert.AreEqual(0, chains.Counted(player, AchievementType.MissionDone), "a mission that needs two is not finished by one");

        await missions.OnItemCollectedAsync(player, item, 1, CancellationToken.None);
        await missions.OnItemCollectedAsync(player, item, 1, CancellationToken.None);

        Assert.AreEqual(1, chains.Counted(player, AchievementType.MissionDone), "finished once, and a finished mission counts no more");
    }

    [TestMethod]
    public void RepairsCountTheGoldTheyCost()
    {
        using var f = new ItemSetupFixture();
        var chains = new AchievementChains(f.World, (AchievementType.RepairCost, 0));

        chains.Enter(f.Player);

        var repairs = new ItemRepairService(f.World, f.Inventory, f.Repairs, achievements: chains.Manager);

        f.Weapon.Endurance = 50;
        f.Gold = 1_000_000;

        repairs.RepairOne(f.Player, ItemSetupFixture.SlotOf(f.Weapon));

        var spent = 1_000_000 - f.Gold;

        Assert.IsTrue(spent > 0, "the tables price the repair");
        Assert.AreEqual(spent, chains.Counted(f.Player, AchievementType.RepairCost));
    }

    [TestMethod]
    public void AnUpgradeCountsAsTriedAndAsFailedWhenTheItemDoesNotGoUp()
    {
        using var f = new ItemSetupFixture(withServerTables: true);
        var chains = new AchievementChains(f.World, (AchievementType.UpgradeTried, 0), (AchievementType.UpgradeFailed, 0));

        chains.Enter(f.Player);

        var costs = ItemSetupFixture.Load<ReinforceRow>("tb_Reinforce");
        var chances = ItemSetupFixture.Load<ReinforceChanceRow>("tb_Reinforce", ItemSetupFixture.ServerTablesVariable);
        var upgrades = new ItemUpgradeService(f.Inventory, costs, chances, new ItemSetupAnswers(f.World), f.Random, achievements: chains.Manager);

        var costById = costs.GroupBy(e => e.Id).ToDictionary(e => e.Key, e => e.First());
        var chanceById = chances.GroupBy(e => e.Id).ToDictionary(e => e.Key, e => e.First());
        var item = ItemSetupFixture.Load<ItemRow>("tb_Item").First(e =>
            e.Reinforce != 0 && costById.ContainsKey(e.Reinforce) && chanceById.ContainsKey(e.Reinforce) && f.Catalog.HomeOf(e.Id) == StorageGroup.Common);
        var target = f.Give(item.Id, 1);

        var chance = chanceById[item.Reinforce];
        var level = Enumerable.Range(ItemUpgradeService.ProtectedFrom, 5)
            .First(e => e < costById[item.Reinforce].MaxLevel && chance.Up[e] > 0 && chance.Break[e] > 0);
        var cost = costById[item.Reinforce].Cost(level)!.Value;

        f.Gold = 100_000_000;
        f.World.Get<CurrencyComponent>(f.Player)[ShopCurrency.Bp] = 1_000_000;
        f.World.Get<CurrencyComponent>(f.Player)[ShopCurrency.Ether] = 1_000_000;

        if (cost.Material != 0)
        {
            f.Give(cost.Material, cost.MaterialCount * 2);
        }

        if (cost.ExtraMaterial != 0)
        {
            f.Give(cost.ExtraMaterial, cost.ExtraMaterialCount * 2);
        }

        // The highest roll that goes up, then the lowest that does not.
        target.Upgrade = (byte)level;
        f.Random.Roll = chance.Up[level];
        upgrades.Upgrade(f.Player, false, default, ItemSetupFixture.SlotOf(target));

        Assert.AreEqual(1, chains.Counted(f.Player, AchievementType.UpgradeTried));
        Assert.AreEqual(0, chains.Counted(f.Player, AchievementType.UpgradeFailed), "an item that went up did not fail");

        target.Upgrade = (byte)level;
        f.Random.Roll = chance.Up[level] + 1;
        upgrades.Upgrade(f.Player, false, default, ItemSetupFixture.SlotOf(target));

        Assert.AreEqual(2, chains.Counted(f.Player, AchievementType.UpgradeTried));
        Assert.AreEqual(1, chains.Counted(f.Player, AchievementType.UpgradeFailed));
    }

    [TestMethod]
    public void ARequestToMakeOrToTakeApartCountsOnceWhateverTheCountOfItems()
    {
        using var f = new ItemSetupFixture();
        var chains = new AchievementChains(f.World, (AchievementType.ItemMade, 0), (AchievementType.ItemDisassembled, 0));

        chains.Enter(f.Player);

        var answers = new ItemSetupAnswers(f.World);
        var recipe = ItemSetupFixture.Load<MakeRow>("tb_Make").First(e =>
            e.Gold > 0 && e.Material1 != 0 && e.Product != e.Material1 && f.Catalog.Exists(e.Product) && f.Catalog.HomeOf(e.Product) == StorageGroup.Common &&
            e.Materials.All(m => m.Item == 0 || f.Catalog.Exists(m.Item)));

        foreach (var (material, count) in recipe.Materials.Where(e => e.Item != 0))
        {
            f.Give(material, count * 2);
        }

        f.Gold = recipe.Gold * 2L;

        new ItemMakeService(f.Inventory, ItemSetupFixture.Load<MakeRow>("tb_Make"), answers, achievements: chains.Manager).Make(f.Player, recipe.Id, 2);

        Assert.AreEqual(2, f.Inventory.CountOf(f.Player, recipe.Product), "two items were made");
        Assert.AreEqual(1, chains.Counted(f.Player, AchievementType.ItemMade), "by one request");

        var results = ItemSetupFixture.Load<DisassembleResultRow>("tb_Disassemble", ItemSetupFixture.ServerTablesVariable);
        var byId = results.GroupBy(e => e.Id).ToDictionary(e => e.Key, e => e.First());
        var row = ItemSetupFixture.Load<ItemRow>("tb_Item").First(e =>
            e.Disassemble != 0 && byId.TryGetValue(e.Disassemble, out var r) && r.Items[0] != 0 && r.Rates[0] > 0 &&
            r.MinCounts[0] > 0 && f.Catalog.Exists(r.Items[0]) && f.Catalog.HomeOf(e.Id) == StorageGroup.Common);
        var disassemble = new ItemDisassembleService(f.Inventory, results, answers, new LowestRandom(), achievements: chains.Manager);

        f.Give(row.Id, 2);

        // A stack is named once for each of its items.
        disassemble.DisassembleMany(
            f.Player, f.Bag.Items.Where(e => e.Item == row.Id).SelectMany(e => Enumerable.Repeat(ItemSetupFixture.SlotOf(e), e.Count)).ToArray());

        Assert.AreEqual(0, f.Inventory.CountOf(f.Player, row.Id), "both items were taken apart");
        Assert.AreEqual(1, chains.Counted(f.Player, AchievementType.ItemDisassembled), "by one request");
    }
}
