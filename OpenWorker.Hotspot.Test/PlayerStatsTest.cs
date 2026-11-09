using System.Buffers.Binary;
using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.ItemSetup;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Gameplay.Modules.Skill.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Res.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The stats of a player come from everything it wears, and the player is told when they change. The tables are made by the
/// test, so that each number can be worked out by hand: a hero with 100 physical attack, 20 physical defence and 1000 life.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class PlayerStatsTest
{
    private const int Weapon = 100;
    private const int Helmet = 200;
    private const int Gem = 300;
    private const int Broach = 400;
    private const int Set = 7;

    private const int HeroAttack = 100;
    private const int HeroDefense = 20;
    private const int HeroHealth = 1000;

    private const int WeaponAttack = 100;
    private const int HelmetDefense = 50;

    private sealed class Fixture : IDisposable
    {
        public World World { get; } = World.Create();
        public RecordingConnection Connection { get; } = new();
        public ItemCatalog Catalog { get; }
        public CombatTables Tables { get; }
        public PlayerStatsService Stats { get; }
        public InventoryService Items { get; }
        public Inventory Inventory { get; } = new(7);
        public Entity Player { get; }

        public Fixture(Func<int, int, int>? roll = null)
        {
            Catalog = new ItemCatalog(
                Rows(
                    TestRows.Build<ItemRow>(("Id", Weapon), ("Classify", 1), ("Level", (short)1), ("Rank", (byte)1), ("PhysicalAttack", WeaponAttack),
                        ("Endurance", (byte)100), ("Reinforce", (short)1), ("SetItem", Set), ("Socket", 1), ("Option", 11)),
                    TestRows.Build<ItemRow>(("Id", Helmet), ("Classify", 2), ("Level", (short)1), ("Rank", (byte)1), ("PhysicalDefense", HelmetDefense),
                        ("Endurance", (byte)100), ("SetItem", Set)),
                    TestRows.Build<ItemRow>(("Id", Gem), ("Classify", 3), ("StatType1", (short)StatEffects.PhysicalDefenseNumber), ("StatValue1", 25)),
                    TestRows.Build<ItemRow>(("Id", Broach), ("Classify", 4), ("Rank", (byte)2), ("EffectType", (short)6), ("EffectValue", 1),
                        ("StatType1", (short)StatEffects.HealthNumber), ("StatValue1", 100))),
                Rows(
                    TestRows.Build<ItemClassifyRow>(("Id", 1), ("ItemUseType", ItemUseType.ABILITY), ("SlotType", (byte)1), ("RepairType", (byte)1)),
                    TestRows.Build<ItemClassifyRow>(("Id", 2), ("ItemUseType", ItemUseType.ABILITY), ("SlotType", (byte)151), ("RepairType", (byte)1)),
                    TestRows.Build<ItemClassifyRow>(("Id", 3), ("ItemUseType", ItemUseType.ITEM_SOCKET_EQUIP), ("SubCategory", (byte)1)),
                    TestRows.Build<ItemClassifyRow>(("Id", 4), ("ItemUseType", ItemUseType.ITEM_BROACH))),
                Rows<InvenSlotExtendRow>(),
                Rows<BankSlotExtendRow>());

            // The penalty of a piece worn down to nothing is all of its stats; repairing one costs ten gold.
            var repairs = Rows(
                TestRows.Build<ItemRepairRow>(("Id", 0), ("EndurancePenalty", 100f), ("GearByEndurance", 10f), ("WeaponByEndurance", 10f)),
                TestRows.Build<ItemRepairRow>(("Id", 1), ("GearByLevel", 1f), ("GearByRank", 1f), ("WeaponByLevel", 1f), ("WeaponByRank", 1f)));

            var gear = new GearStats(
                Catalog,
                Rows(TestRows.Build<ReinforceRow>(("Id", 1), ("StatRate01", 10f), ("StatRate02", 20f))),
                Rows(TestRows.Build<ItemSetItemRow>(("Id", Set), ("Count", (byte)2), ("Item1", Weapon), ("Item2", Helmet),
                    ("FirstTypeOf2", (short)StatEffects.HealthShare), ("FirstValueOf2", 10f))),
                repairs,
                Rows(TestRows.Build<BroachSetRow>(("Id", (short)11222), ("Buff", (short)27001))),
                Rows(TestRows.Build<BuffRow>(("Id", (ushort)27001), ("Stat1", (byte)StatEffects.PhysicalAttackShare), ("Value1", 10))));

            Tables = new CombatTables(
                Rows<MonsterRow>(),
                Rows(TestRows.Build<StatusRow>(("Id", (short)1), ("StrengthFirst", 10f), ("AgilityFirst", 10f), ("PhysicalAttackRate", 1f),
                    ("PhysicalDefenseRate", 2f), ("HealthFirst", (float)HeroHealth))),
                Rows<SkillRow>(),
                Rows(TestRows.Build<CharacterInfoRow>(("Id", (short)1000), ("Character_Status", 1), ("Character_Status_Type", (byte)1))),
                items: Catalog,
                gear: gear);

            Stats = new PlayerStatsService(World, Tables);

            var options = new ItemOptionRoller(
                Rows(OptionRow()),
                Rows(TestRows.Build<SocketRow>(("Id", 1), ("MaxSockets", (byte)3), ("FixedSockets", (byte)1))),
                roll: roll);

            // Items are given no options unless the test fixes the rolls.
            Items = new InventoryService(World, Catalog, new LocalItemSerials(), Stats, options: roll is null ? null : options);
            Repairs = new ItemRepairService(World, Items, repairs, stats: Stats);
            Wear = new EnduranceWear(World, Catalog, Rows(EnduranceRow()), Stats, roll: (_, _) => 1);

            Inventory.Add(StorageGroup.AbilityEquip, 20);
            Inventory.Add(StorageGroup.ShapeEquip, 20);
            Inventory.Add(StorageGroup.Common, 20);

            var currency = new CurrencyComponent { Gold = 1000 };

            Player = World.Create(
                new ServerSessionComponent(Connection),
                new ActorComponent { Identifier = 7 },
                new PersonInfoComponent { Name = "Test", Hero = Hero.Haru },
                new LevelComponent(1, 0),
                new InventoryComponent(Inventory),
                currency,
                new AbilityComponent
                {
                    Health = new AbilityComponentEntry { Current = HeroHealth, Max = HeroHealth },
                    SoulGain = default,
                    SoulVapor = default,
                    Stamina = default,
                    SuperArmor = default,
                    Speed = new SpeedComponentEntry { Move = 1f, Attack = 1f }
                });
        }

        public ItemRepairService Repairs { get; }

        public EnduranceWear Wear { get; }

        public ItemInstance PutOn(int item, AbilityEquipSlot slot)
        {
            var instance = Items.Create(item, 1);

            Inventory.Place(StorageGroup.AbilityEquip, (int)slot, instance);

            return instance;
        }

        public CombatStats Now => Tables.GetPlayer(World, Player);

        /// <summary>
        /// The stats in the stat lists (03/34) the player has been sent so far, the last value of each.
        /// </summary>
        public async Task<Dictionary<STAT_TYPE, float>> SentAsync(int lists)
        {
            await MetricAssertions.UntilAsync(() => Lists().Count() >= lists, "the stat lists reach the player");

            var sent = new Dictionary<STAT_TYPE, float>();

            foreach (var body in Lists())
            {
                var count = body[5];

                for (var i = 0; i < count; i++)
                {
                    var at = 6 + i * 6;

                    sent[(STAT_TYPE)BinaryPrimitives.ReadUInt16LittleEndian(body.AsSpan(at + 4))] = BinaryPrimitives.ReadSingleLittleEndian(body.AsSpan(at));
                }
            }

            return sent;
        }

        private IEnumerable<byte[]> Lists()
        {
            return Connection.Frames
                .Select(PlayerFrame.Open)
                .Where(e => e.Is(GroupOpcode.Character, (byte)CharacterOpcode.UpdateStatList))
                .Select(e => e.Body);
        }

        public void Dispose() => World.Dispose();
    }

    [TestMethod]
    public async Task WornGearAddsItsStatsAndTakingItOffTakesThemAway()
    {
        using var f = new Fixture();

        f.Stats.SendAll(f.Player);

        var sent = await f.SentAsync(1);

        Assert.AreEqual(HeroAttack, sent[STAT_TYPE.STAT_PATK_MAX], "the whole list at entry");

        var weapon = f.PutOn(Weapon, AbilityEquipSlot.SoulWeapon);

        f.Stats.Send(f.Player);
        sent = await f.SentAsync(2);

        Assert.AreEqual(HeroAttack + WeaponAttack, sent[STAT_TYPE.STAT_PATK_MAX]);
        Assert.AreEqual((HeroAttack + WeaponAttack) * 0.8f, sent[STAT_TYPE.STAT_PATK_MIN], 0.01f);

        f.Inventory.Lift(StorageGroup.AbilityEquip, weapon.Slot);
        f.Stats.Send(f.Player);
        sent = await f.SentAsync(3);

        Assert.AreEqual(HeroAttack, sent[STAT_TYPE.STAT_PATK_MAX]);

        f.Stats.Send(f.Player);
        await Task.Delay(100);

        Assert.AreEqual(3, f.Connection.Frames.Select(PlayerFrame.Open).Count(e => e.Is(GroupOpcode.Character, (byte)CharacterOpcode.UpdateStatList)),
            "nothing changed, nothing is sent");
    }

    [TestMethod]
    public async Task TheStatsSentToTheClientAreTheOnesTheFightUses()
    {
        using var f = new Fixture();

        f.PutOn(Weapon, AbilityEquipSlot.SoulWeapon).Upgrade = 2;
        f.PutOn(Helmet, AbilityEquipSlot.HeadGear);
        f.Stats.SendAll(f.Player);

        var sent = await f.SentAsync(1);
        var fight = f.Now;

        foreach (var (type, value) in fight.Values())
        {
            Assert.AreEqual(value, sent[type], type.ToString());
        }

        Assert.AreEqual(fight.MaxHealth, f.World.Get<AbilityComponent>(f.Player).Health.Max, "the life the fight takes from");
    }

    [TestMethod]
    public void AnUpgradeAGemAndABroachEachAddTheStatTheyName()
    {
        using var f = new Fixture();

        var weapon = f.PutOn(Weapon, AbilityEquipSlot.SoulWeapon);
        var helmet = f.PutOn(Helmet, AbilityEquipSlot.HeadGear);
        var dressed = f.Now;

        // +2 adds 20 % of the attack of the weapon.
        weapon.Upgrade = 2;

        Assert.AreEqual(dressed.Physical.AttackMax + WeaponAttack * 0.2f, f.Now.Physical.AttackMax, 0.01f);

        // A gem of physical defence in an open socket adds its 25; in a closed one nothing.
        helmet.SetSocket(0, new ItemSocket(Gem, new ItemOption[5]));

        Assert.AreEqual(dressed.Physical.Defense, f.Now.Physical.Defense, "the socket is closed");

        helmet.SocketActive = 1;

        Assert.AreEqual(dressed.Physical.Defense + 25f, f.Now.Physical.Defense);

        // Three broaches of one theme: each adds its 100 life, and the group gives the buff of its set, +10 % attack.
        var clothing = f.Items.Create(Broach, 1);

        clothing.Broaches = [Broach, Broach, Broach, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1];
        f.Inventory.Place(StorageGroup.ShapeEquip, 0, clothing);

        var attack = f.Tables.GetPlayer(f.World, f.Player).Physical.AttackMax;

        Assert.AreEqual((HeroAttack * 1.1f + WeaponAttack + WeaponAttack * 0.2f), attack, 0.01f);
        Assert.AreEqual(HeroHealth * 1.1f + 300f, f.Now.MaxHealth, 0.01f, "the broaches, and the set of the weapon and the helmet");
    }

    [TestMethod]
    public void ASetGivesItsBonusFromTheCountOfPiecesItNamesAndLosesItBelow()
    {
        using var f = new Fixture();

        f.PutOn(Weapon, AbilityEquipSlot.SoulWeapon);

        Assert.AreEqual(HeroHealth, f.Now.MaxHealth, "one piece of two");

        var helmet = f.PutOn(Helmet, AbilityEquipSlot.HeadGear);

        Assert.AreEqual(HeroHealth * 1.1f, f.Now.MaxHealth, 0.01f, "two pieces of two");

        f.Inventory.Lift(StorageGroup.AbilityEquip, helmet.Slot);

        Assert.AreEqual(HeroHealth, f.Now.MaxHealth);
    }

    [TestMethod]
    public async Task GearWearsOnAHitAndAtZeroItsStatsAreGoneUntilRepaired()
    {
        using var f = new Fixture();

        var helmet = f.PutOn(Helmet, AbilityEquipSlot.HeadGear);

        f.Stats.SendAll(f.Player);
        f.Wear.Hit(f.Player);

        Assert.AreEqual(99, helmet.Endurance, "a hit takes the loss of the table");
        Assert.AreEqual(HeroDefense + HelmetDefense, f.Now.Physical.Defense, "the repair table takes nothing at 99");

        await MetricAssertions.UntilAsync(
            () => f.Connection.Frames.Select(PlayerFrame.Open).Any(e => e.Is(GroupOpcode.ItemSetup, (byte)ItemSetupOpcode.Endurance)),
            "the client is told the new endurance");

        helmet.Endurance = 1;
        f.Wear.Hit(f.Player);

        Assert.AreEqual(0, helmet.Endurance);
        Assert.AreEqual(HeroDefense, f.Now.Physical.Defense, "nothing is left of a piece worn to nothing");

        var sent = await f.SentAsync(2);

        Assert.AreEqual(HeroDefense, sent[STAT_TYPE.STAT_PDEF], "the player is told");

        f.Wear.Hit(f.Player);

        Assert.AreEqual(0, helmet.Endurance, "what is spent wears no further");

        f.Repairs.RepairOne(f.Player, new StorageSlot { Storage = StorageGroup.AbilityEquip, Slot = helmet.Slot });

        Assert.AreEqual(100, helmet.Endurance);

        sent = await f.SentAsync(3);

        Assert.AreEqual(HeroDefense + HelmetDefense, sent[STAT_TYPE.STAT_PDEF], "a repair gives the stats back");
    }

    [TestMethod]
    public void ANewItemGetsTheOptionsAndTheOpenSocketsOfItsRows()
    {
        // Every roll gives its lowest bound: three options of three, and each its lowest value.
        using var f = new Fixture(roll: (low, _) => low);

        var weapon = f.Items.Create(Weapon, 1);

        Assert.AreEqual(1, weapon.SocketActive, "the sockets open from the start");
        CollectionAssert.AreEquivalent(new short[] { 33, 37, 44 }, weapon.Options.Take(3).Select(e => e.Type).ToArray());
        Assert.AreEqual(5, weapon.Options.Single(e => e.Type == 33).Option);
        Assert.AreEqual(0, weapon.Options[3].Type);

        f.Inventory.Place(StorageGroup.AbilityEquip, 0, weapon);

        // The rolled physical attack (33) adds to the attack.
        Assert.AreEqual(HeroAttack + WeaponAttack + 5, f.Now.Physical.AttackMax, 0.01f);
    }

    /// <summary>
    /// A row of the server's option table: three options of three listed, physical attack (5..9), physical defence (2..4) and
    /// physical critical chance (1..3).
    /// </summary>
    private static ItemOptionRollRow OptionRow()
    {
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);

        writer.Write(11);
        writer.Write((byte)3);
        writer.Write((byte)3);
        writer.Write((byte)3);

        ushort[] types = [33, 37, 44];
        int[] lows = [5, 2, 1];
        int[] highs = [9, 4, 3];

        for (var i = 0; i < ItemOptionRollRow.Options; i++)
        {
            writer.Write(i < types.Length ? types[i] : (ushort)0);
        }

        for (var i = 0; i < ItemOptionRollRow.Options; i++)
        {
            writer.Write(i < lows.Length ? lows[i] : 0);
        }

        for (var i = 0; i < ItemOptionRollRow.Options; i++)
        {
            writer.Write(i < highs.Length ? highs[i] : 0);
        }

        writer.Write(0);
        stream.Position = 0;

        return new ItemOptionRollRow(new BinaryReader(stream));
    }

    /// <summary>
    /// The wear of the hero: every hit taken costs the armour one point.
    /// </summary>
    private static ItemEnduranceRow EnduranceRow()
    {
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);

        writer.Write((int)Hero.Haru);
        writer.Write((ushort)0);
        writer.Write((ushort)0);
        writer.Write(0f);
        writer.Write(0f);
        writer.Write((ushort)0);
        writer.Write((ushort)0);
        writer.Write(0f);
        writer.Write(0f);
        writer.Write((ushort)10_000);
        writer.Write(1f);
        writer.Write((ushort)0);
        writer.Write(0f);
        stream.Position = 0;

        return new ItemEnduranceRow(new BinaryReader(stream));
    }

    private static ReadOnlyCollection<T> Rows<T>(params T[] rows) => new(rows);
}
