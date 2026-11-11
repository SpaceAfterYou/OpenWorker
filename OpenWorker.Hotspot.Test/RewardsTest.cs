using System.Collections.ObjectModel;
using System.Numerics;
using Microsoft.Extensions.Logging.Abstractions;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Boosters;
using OpenWorker.Gameplay.Modules.Boosters.Components;
using OpenWorker.Gameplay.Modules.Drops;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persons;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.UpdateContent.Res.Rows;
using OpenWorker.Gameplay.Modules.Persistence;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// Experience, levels and drops, against the tables of the client for items and levels and rows made here for the server ones.
/// Set OPENWORKER_TEST_RES to the folder with the *.res files.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class RewardsTest
{
    private const string TablesVariable = "OPENWORKER_TEST_RES";

    private const int Material = 710105001;

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

    private static ReadOnlyCollection<T> Rows<T>(params Action<BinaryWriter>[] rows)
    {
        return new ReadOnlyCollection<T>(rows.Select(write =>
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            write(writer);
            writer.Flush();
            stream.Position = 0;

            return (T)Activator.CreateInstance(typeof(T), new BinaryReader(stream))!;
        }).ToArray());
    }

    private static MonsterRow FindMonster()
    {
        return Load<MonsterRow>("tb_Monster").First(e => e.Level > 1 && e.Type == 0 && e.Rank <= 6 && e.DropId != 0);
    }

    private static Action<BinaryWriter> MonsterExp(byte level, int rank, int value)
    {
        return writer =>
        {
            writer.Write(level);
            writer.Write(level);

            for (var i = 0; i < 7; i++)
            {
                writer.Write(i == rank ? value : 0);
            }
        };
    }

    private static Action<BinaryWriter> Drop(int id, ushort goldChance, ushort goldMin, ushort goldMax, int group, ushort groupChance)
    {
        return writer =>
        {
            writer.Write(id);
            writer.Write((byte)0);
            writer.Write(goldChance);
            writer.Write(goldMin);
            writer.Write(goldMax);
            writer.Write((ushort)0);
            writer.Write((byte)0);
            writer.Write(new byte[2 * 3]);     // soul vapor
            writer.Write(new byte[2 * 6]);     // ether
            writer.Write(new byte[2 * 3]);     // battle points

            for (var i = 0; i < DropRow.GroupCount; i++)
            {
                writer.Write(i == 0 ? groupChance : (ushort)0);
            }

            for (var i = 0; i < DropRow.GroupCount; i++)
            {
                writer.Write(i == 0 ? group : 0);
            }

            writer.Write(new byte[2 * DropRow.CharacterGroupCount + 4 * DropRow.CharacterGroupCount]);
            writer.Write(new byte[4 * 2 + 2 * 2 + 2 + 4 * 2 + 2 * 2 + 2 * 2]);
        };
    }

    private static Action<BinaryWriter> Group(int id, int item, int min, int max)
    {
        return writer =>
        {
            writer.Write(id);

            for (var i = 0; i < DropGroupRow.ItemCount; i++)
            {
                writer.Write(i == 0 ? (ushort)10000 : (ushort)0);
            }

            for (var i = 0; i < DropGroupRow.ItemCount; i++)
            {
                writer.Write(i == 0 ? item : 0);
            }

            for (var i = 0; i < DropGroupRow.ItemCount; i++)
            {
                writer.Write(i == 0 ? (byte)min : (byte)0);
            }

            for (var i = 0; i < DropGroupRow.ItemCount; i++)
            {
                writer.Write(i == 0 ? (byte)max : (byte)0);
            }
        };
    }

    private sealed class Fixture : IDisposable
    {
        public World World { get; } = World.Create();
        public InventoryService Inventory { get; }
        public ExperienceService Experience { get; }
        public DropService Drops { get; }
        public Entity Player { get; }
        public ReadOnlyCollection<MonsterRow> Monsters { get; } = Load<MonsterRow>("tb_Monster");

        public Fixture(byte level = 1, ReadOnlyCollection<MonsterExpRow>? monsterExp = null, ReadOnlyCollection<PartyExpMobRow>? intervals = null,
            ReadOnlyCollection<DropRow>? drops = null, ReadOnlyCollection<DropGroupRow>? groups = null,
            ReadOnlyCollection<DropRateLevelRow>? levelRates = null, ReadOnlyCollection<DropRateMemberRow>? memberRates = null,
            BoosterEffectValue? boost = null)
        {
            // The person carries one booster with the effect, when a test asks for one.
            var boosters = boost is { } effect
                ? new BoosterManager(World, null!, BoosterCatalog.From([new BoosterDefinition(1, 1, BoosterTime.Endless, 0, [effect])]), TimeProvider.System, NullLogger<BoosterManager>.Instance)
                : null;

            var catalog = new ItemCatalog(
                Load<ItemRow>("tb_Item"),
                Load<ItemClassifyRow>("tb_Item_Classify"),
                Load<InvenSlotExtendRow>("tb_Inven_Slot_Extend"),
                Load<BankSlotExtendRow>("tb_Bank_Slot_Extend"));

            var serials = new LocalItemSerials();

            Inventory = new InventoryService(World, catalog, serials);
            Experience = new ExperienceService(
                World,
                Inventory,
                Load<LevelupPointRow>("tb_Levelup_Point"),
                monsterExp ?? Rows<MonsterExpRow>(),
                intervals ?? Rows<PartyExpMobRow>(),
                Rows<MonsterRewardRow>(),
                Monsters,
                boosters: boosters);

            Drops = new DropService(
                World,
                Inventory,
                catalog,
                serials,
                drops ?? Rows<DropRow>(),
                groups ?? Rows<DropGroupRow>(),
                Rows<DropRateMobRow>(),
                Monsters,
                levelRates: levelRates,
                memberRates: memberRates,
                boosters: boosters);

            Player = World.Create(
                Component.GetComponentType(typeof(ActorComponent)),
                Component.GetComponentType(typeof(InventoryComponent)),
                Component.GetComponentType(typeof(LevelComponent)),
                Component.GetComponentType(typeof(PersonInfoComponent)),
                Component.GetComponentType(typeof(WorldComponent)),
                Component.GetComponentType(typeof(CurrencyComponent)),
                Component.GetComponentType(typeof(DirtyComponent)));

            World.Set(Player, new DirtyComponent(new PersonDirty(7)));

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

            var factory = new InventoryFactory(catalog, Load<CreateClothRow>("tb_Create_Cloth"), serials);

            World.Set(Player, new InventoryComponent(factory.CreateNew(person)));
            World.Set(Player, new LevelComponent(level, 0));
            World.Set(Player, new PersonInfoComponent { Name = "Test", Hero = Hero.Haru });
            World.Set(Player, new WorldComponent { Location = 0, Position = Vector3.Zero, Rotation = 0, Jump = 0 });
            World.Set(Player, new CurrencyComponent());

            if (boosters is not null)
            {
                World.Add(Player, new BoosterComponent { Person = 7, Book = new BoosterBook { Boosters = [new BoosterEntry { Booster = 1 }] } });
            }
        }

        public LevelComponent Rank => World.Get<LevelComponent>(Player);


        public void Dispose()
        {
            World.Dispose();
        }
    }

    [TestMethod]
    public void ExperienceBelowTheNeedStaysInTheLevel()
    {
        using var f = new Fixture();

        f.Experience.Add(f.Player, 100);

        Assert.AreEqual(1, f.Rank.Level);
        Assert.AreEqual(100, f.Rank.Experience);
        Assert.IsTrue(f.World.DirtyOf(f.Player)!.Sections.HasFlag(PersonSections.Level), "A change of experience is saved");
    }

    [TestMethod]
    public void WhatIsLeftAfterALevelCountsTowardTheNextOne()
    {
        using var f = new Fixture();

        var first = f.Experience.Need(1);
        var second = f.Experience.Need(2);

        f.Experience.Add(f.Player, first + second + 5);

        Assert.AreEqual(3, f.Rank.Level);
        Assert.AreEqual(5, f.Rank.Experience);
    }

    [TestMethod]
    public void NothingIsGainedOnTheLastLevel()
    {
        using var f = new Fixture(Load<LevelupPointRow>("tb_Levelup_Point").Max(e => e.Id));

        f.Experience.Add(f.Player, 1000);

        Assert.AreEqual(f.Experience.MaxLevel, f.Rank.Level);
        Assert.AreEqual(0, f.Rank.Experience);
    }

    [TestMethod]
    public void MonsterIsWorthTheExperienceOfItsLevelAndRank()
    {
        var monster = FindMonster();

        using var f = new Fixture(monsterExp: Rows<MonsterExpRow>(MonsterExp((byte)monster.Level, monster.Rank, 40)));

        Assert.AreEqual(0, f.Experience.ForMonster(-1));

        // Without the table of shares the monsters of a sector are worth all of it, anything else nothing.
        Assert.AreEqual(40, f.Experience.ForMonster(monster.Id));

        f.Experience.GiveForMonster(f.Player, monster.Id);

        Assert.AreEqual(40, f.Rank.Experience);
    }

    /// <summary>
    /// A booster of experience adds a part of the experience of a kill, which the client is told as a bonus, and the player gets
    /// the two together.
    /// </summary>
    [TestMethod]
    public void ABoosterAddsToTheExperienceOfAKill()
    {
        var monster = FindMonster();

        using var f = new Fixture(
            monsterExp: Rows<MonsterExpRow>(MonsterExp((byte)monster.Level, monster.Rank, 40)),
            boost: new BoosterEffectValue(BoosterEffect.IncExp, BoosterApply.Rate, 0.25f));

        var given = f.Experience.GiveForMonster(f.Player, monster.Id);

        Assert.AreEqual(50, given, "40 and a quarter of it");
        Assert.AreEqual(50, f.Rank.Experience);
    }

    /// <summary>
    /// A booster of gold adds a part of each heap of gold that falls; the heap is the two together, the bonus is told apart, and the
    /// player takes it whole.
    /// </summary>
    [TestMethod]
    public void ABoosterAddsToTheGoldThatFalls()
    {
        var monster = FindMonster();

        using var f = new Fixture(
            drops: Rows<DropRow>(Drop(monster.DropId, 10000, 100, 100, 0, 0)),
            boost: new BoosterEffectValue(BoosterEffect.IncGoldDrop, BoosterApply.Rate, 0.5f));

        f.Drops.DropForKill(f.Player, monster.Id, Vector3.Zero);

        var heap = f.Drops.On(f.Player).Single();

        Assert.AreEqual(150, heap.Count);
        Assert.AreEqual(50, heap.Bonus);
        Assert.IsTrue(f.Drops.PickUp(f.Player, heap.Serial));
        Assert.AreEqual(150, f.Inventory.Gold(f.Player));
    }

    /// <summary>
    /// A booster of items makes what a group lets fall more likely: a chance of 5000 that loses to a roll of 6000 wins with half
    /// as much again.
    /// </summary>
    [TestMethod]
    public void ABoosterRaisesTheChanceOfWhatFalls()
    {
        var monster = FindMonster();
        var roll = new FixedRandom(6000);

        using var plain = new Fixture(
            drops: Rows<DropRow>(Drop(monster.DropId, 0, 0, 0, group: 5, groupChance: 5000)),
            groups: Rows<DropGroupRow>(Group(5, Material, 1, 1)));

        using var boosted = new Fixture(
            drops: Rows<DropRow>(Drop(monster.DropId, 0, 0, 0, group: 5, groupChance: 5000)),
            groups: Rows<DropGroupRow>(Group(5, Material, 1, 1)),
            boost: new BoosterEffectValue(BoosterEffect.IncItemDrop, BoosterApply.Rate, 0.5f));

        plain.Drops.DropForKill(plain.Player, monster.Id, Vector3.Zero, roll);
        boosted.Drops.DropForKill(boosted.Player, monster.Id, Vector3.Zero, roll);

        Assert.AreEqual(0, plain.Drops.On(plain.Player).Count);
        Assert.AreEqual(Material, boosted.Drops.On(boosted.Player).Single().ObjectId);
    }

    [TestMethod]
    public void ExperienceIsLessForAMonsterThePlayerHasOutgrown()
    {
        var monster = FindMonster();
        var interval = (sbyte)(10 - monster.Level);

        using var f = new Fixture(
            level: 10,
            monsterExp: Rows<MonsterExpRow>(MonsterExp((byte)monster.Level, monster.Rank, 100)),
            intervals: Rows<PartyExpMobRow>(writer =>
            {
                writer.Write(interval);
                writer.Write(0.5f);
            }));

        f.Experience.GiveForMonster(f.Player, monster.Id);

        Assert.AreEqual(50, f.Rank.Experience);
    }

    [TestMethod]
    public void MonsterDropsMoneyAndAnItemAndThePlayerTakesThem()
    {
        var monster = FindMonster();

        using var f = new Fixture(
            drops: Rows<DropRow>(Drop(monster.DropId, 10000, 30, 30, group: 5, groupChance: 10000)),
            groups: Rows<DropGroupRow>(Group(5, Material, 4, 4)));

        f.Drops.DropForKill(f.Player, monster.Id, new Vector3(10, 10, 0));

        var lying = f.Drops.On(f.Player).ToArray();

        CollectionAssert.AreEquivalent(
            new[] { DropService.GoldObject, Material },
            lying.Select(e => e.ObjectId).ToArray());
        Assert.IsTrue(lying.All(e => e.Group == lying[0].Group), "One kill, one group");

        foreach (var drop in lying)
        {
            Assert.IsTrue(f.Drops.PickUp(f.Player, drop.Serial));
        }

        Assert.AreEqual(30, f.Inventory.Gold(f.Player));
        Assert.AreEqual(4, f.Inventory.CountOf(f.Player, Material));
        Assert.AreEqual(0, f.Drops.On(f.Player).Count);
    }

    /// <summary>
    /// Every player in the maze gets a drop of their own, rolled for them; a party's drop is multiplied by how much the levels
    /// of its members differ and by how many of them there are, and a player alone by neither.
    /// </summary>
    [TestMethod]
    public void EveryMemberOfAPartyGetsADropOfTheirOwnAtTheRateOfTheParty()
    {
        var monster = FindMonster();

        using var f = new Fixture(
            level: 10,
            drops: Rows<DropRow>(Drop(monster.DropId, 6000, 30, 30, 0, 0)),
            levelRates: Rows<DropRateLevelRow>(Rate(0, 1f), Rate(5, 0.5f)),
            memberRates: Rows<DropRateMemberRow>(Rate(1, 1f), Rate(2, 1f)));

        var other = f.World.Create(new LevelComponent(5, 0));
        var roll = new FixedRandom(4000);

        // A gap of five levels halves the chance of 6000 to 3000, below the roll of 4000: nothing falls for either of them.
        f.Drops.DropForKill([f.Player, other], monster.Id, Vector3.Zero, roll);

        Assert.AreEqual(0, f.Drops.On(f.Player).Count + f.Drops.On(other).Count);

        // Two members of one level fall in with the chance whole, and each has their own, with their own serial.
        f.World.Set(other, new LevelComponent(10, 0));
        f.Drops.DropForKill([f.Player, other], monster.Id, Vector3.Zero, roll);

        var mine = f.Drops.On(f.Player).Single();
        var theirs = f.Drops.On(other).Single();

        Assert.AreEqual(DropService.GoldObject, mine.ObjectId);
        Assert.AreEqual(DropService.GoldObject, theirs.ObjectId);
        Assert.AreNotEqual(mine.Serial, theirs.Serial);
        Assert.IsFalse(f.Drops.PickUp(other, mine.Serial), "Nobody takes what lies for another");

        // The same gap means nothing to a player alone.
        f.World.Set(other, new LevelComponent(5, 0));
        f.Drops.Forget(f.Player);
        f.Drops.DropForKill([f.Player], monster.Id, Vector3.Zero, roll);

        Assert.AreEqual(1, f.Drops.On(f.Player).Count);
    }

    private static Action<BinaryWriter> Rate(byte key, float rate)
    {
        return writer =>
        {
            writer.Write(key);
            writer.Write(rate);
        };
    }

    /// <summary>
    /// A random that gives the same roll to every chance, so that a drop falls or does not by its rate alone.
    /// </summary>
    private sealed class FixedRandom(int roll) : Random
    {
        public override int Next(int minValue, int maxValue) => maxValue == 10001 ? roll : minValue;
    }

    [TestMethod]
    public void ThingsFarAwayAreNotTaken()
    {
        var monster = FindMonster();

        using var f = new Fixture(drops: Rows<DropRow>(Drop(monster.DropId, 10000, 30, 30, 0, 0)));

        f.Drops.DropForKill(f.Player, monster.Id, new Vector3(100000, 0, 0));

        var gold = f.Drops.On(f.Player).Single();

        Assert.IsFalse(f.Drops.PickUp(f.Player, gold.Serial));
        Assert.AreEqual(0, f.Inventory.Gold(f.Player));
        Assert.AreEqual(1, f.Drops.On(f.Player).Count);
    }

    [TestMethod]
    public void ItemStaysOnTheGroundWhenTheBagIsFull()
    {
        var monster = FindMonster();

        using var f = new Fixture(
            drops: Rows<DropRow>(Drop(monster.DropId, 0, 0, 0, group: 5, groupChance: 10000)),
            groups: Rows<DropGroupRow>(Group(5, Material, 1, 1)));

        var size = f.Inventory.InventoryOf(f.Player)!.Find(OpenWorker.Hotspot.Modules.Items.Enums.StorageGroup.Common)!.Size;

        f.Inventory.TryGive(f.Player, Material, size * 50);
        f.Drops.DropForKill(f.Player, monster.Id, Vector3.Zero);

        var item = f.Drops.On(f.Player).Single();

        Assert.IsFalse(f.Drops.PickUp(f.Player, item.Serial));
        Assert.AreEqual(1, f.Drops.On(f.Player).Count);
    }

    [TestMethod]
    public void NoDropRowMeansNothingFalls()
    {
        using var f = new Fixture();

        f.Drops.DropForKill(f.Player, FindMonster().Id, Vector3.Zero);

        Assert.AreEqual(0, f.Drops.On(f.Player).Count);
    }

    [TestMethod]
    public void GoldBelowTheChanceDoesNotFall()
    {
        var monster = FindMonster();

        using var f = new Fixture(drops: Rows<DropRow>(Drop(monster.DropId, 0, 1, 1, 0, 0)));

        for (var i = 0; i < 20; i++)
        {
            f.Drops.DropForKill(f.Player, monster.Id, Vector3.Zero);
        }

        Assert.AreEqual(0, f.Drops.On(f.Player).Count);
    }
}
