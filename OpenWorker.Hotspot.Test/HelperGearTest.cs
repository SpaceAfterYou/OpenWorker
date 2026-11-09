using System.Buffers.Binary;
using Arch.Core;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Helpers;
using OpenWorker.Gameplay.Modules.Helpers.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Hotspot.Modules.Helpers.Requests;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// What helpers wear, against the tables of the client (OPENWORKER_TEST_RES): items of the helper's number in its three
/// slots, moved between the bag and the helper, and the orb of a friend.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class HelperGearTest
{
    // Mirium, number 100: her first two weapons, her first gear, and a weapon of Ryu (number 101).
    private const int Mirium = 10105166;
    private const int FirstWeapon = 360010102;
    private const int SecondWeapon = 360010103;
    private const int FirstGear = 360010201;
    private const int RyuWeapon = 360020102;

    private sealed class Fixture : IDisposable
    {
        public World World { get; } = World.Create();
        public FakeTimeProvider Time { get; } = new(DateTimeOffset.FromUnixTimeSeconds(1_800_000_000));
        public InventoryService Items { get; }
        public HelperGear Gear { get; }
        public RecordingConnection Connection { get; } = new();
        public Entity Player { get; }

        public Inventory Inventory => Items.InventoryOf(Player)!;

        public Fixture()
        {
            var catalog = new ItemCatalog(
                TestTables.Load<ItemRow>("tb_Item"),
                TestTables.Load<ItemClassifyRow>("tb_Item_Classify"),
                TestTables.Load<InvenSlotExtendRow>("tb_Inven_Slot_Extend"),
                TestTables.Load<BankSlotExtendRow>("tb_Bank_Slot_Extend"));
            var helpers = new HelperCatalog(TestTables.Load<HelperRow>("tb_Helper"));
            var serials = new LocalItemSerials();

            Items = new InventoryService(World, catalog, serials);

            var stats = new HelperStats(helpers, TestTables.Load<MonsterRow>("tb_Monster"), TestTables.Load<StatusRow>("tb_Status"), catalog, Time);

            Gear = new HelperGear(World, helpers, Items, stats, time: Time);

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

            var book = new HelperBook();
            book.TryAdd(Mirium, out _);

            Player = World.Create(
                new ServerSessionComponent(Connection),
                new InventoryComponent(new InventoryFactory(catalog, TestTables.Load<CreateClothRow>("tb_Create_Cloth"), serials).CreateNew(person)),
                new LevelComponent(20, 0),
                new HelperComponent { Person = 7, Book = book });
        }

        public ItemInstance Give(int item)
        {
            Assert.IsTrue(Items.TryGive(Player, item, 1, out var changes));

            return Inventory.At(changes[0].Storage, changes[0].Slot)!;
        }

        public ItemInstance?[] Worn => Gear.Worn(Player, Mirium);

        /// <summary>
        /// Sends the request as the client writes it and returns the code of a refusal, or 0 when the helper was answered.
        /// </summary>
        public async Task<ushort> EquipAsync(ItemInstance? fromBag, ItemInstance? fromHelper, short helperSlot)
        {
            var before = Connection.Frames.Length;
            var bytes = new byte[33];

            bytes[0] = (byte)(fromBag?.Storage ?? StorageGroup.Common);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(1), fromBag?.Item ?? 0);
            BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(5), fromBag?.Slot ?? 0);
            BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(7), fromBag?.Serial ?? 0);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(15), Mirium);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(19), fromHelper?.Item ?? 0);
            BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(23), helperSlot);
            BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(25), fromHelper?.Serial ?? 0);

            using var reader = new BinaryReader(new MemoryStream(bytes));

            Gear.Equip(Player, new HelperEquipRequest(reader));

            await MetricAssertions.UntilAsync(
                () => Connection.Frames.Select(PlayerFrame.Open).Skip(before).Any(e => e.Opcode.Group == (byte)GroupOpcode.Helper),
                "the client is answered");

            var answer = Connection.Frames.Select(PlayerFrame.Open).Skip(before).Last(e => e.Opcode.Group == (byte)GroupOpcode.Helper);

            if (answer.Is(GroupOpcode.Helper, (byte)HelperOpcode.Equip))
            {
                Assert.AreEqual(0, BinaryPrimitives.ReadInt32LittleEndian(answer.Body), "the result of the answer");

                return 0;
            }

            Assert.IsTrue(answer.Is(GroupOpcode.Helper, (byte)HelperOpcode.Equip | 0x80), "an answer to the request");

            return BinaryPrimitives.ReadUInt16LittleEndian(answer.Body);
        }

        public void Dispose() => World.Dispose();
    }

    /// <summary>
    /// An item of the helper's number goes into the slot of its kind, a second one only by a swap, which sends the worn one
    /// to the bag slot the new one left; taking off needs a free bag slot. Every item is in one place only.
    /// </summary>
    [TestMethod]
    public async Task PuttingOnSwappingAndTakingOffKeepOneItemPerSlot()
    {
        using var fixture = new Fixture();

        var first = fixture.Give(FirstWeapon);
        var second = fixture.Give(SecondWeapon);
        var gear = fixture.Give(FirstGear);
        var other = fixture.Give(RyuWeapon);
        var firstSlot = first.Slot;
        var secondSlot = second.Slot;

        Assert.AreEqual(0, await fixture.EquipAsync(first, null, 0));
        Assert.AreSame(first, fixture.Worn[0]);
        Assert.IsNull(fixture.Inventory.At(StorageGroup.Common, firstSlot), "the item left the bag");

        Assert.AreEqual(HelperGear.CannotEquip, await fixture.EquipAsync(second, null, 0), "a taken slot takes another only by a swap");
        Assert.AreEqual(HelperGear.CannotEquip, await fixture.EquipAsync(other, null, 0), "an item of another helper");

        Assert.AreEqual(0, await fixture.EquipAsync(second, first, 0));
        Assert.AreSame(second, fixture.Worn[0]);
        Assert.AreSame(first, fixture.Inventory.At(StorageGroup.Common, secondSlot), "the worn item goes where the new one was");

        Assert.AreEqual(0, await fixture.EquipAsync(gear, null, 1));
        Assert.AreSame(gear, fixture.Worn[1], "a gear goes to the second slot whatever slot the client names");

        Assert.AreEqual(0, await fixture.EquipAsync(null, second, 0));
        Assert.IsNull(fixture.Worn[0]);
        Assert.AreEqual(StorageGroup.Common, second.Storage);

        var serials = fixture.Inventory.Items.Select(e => e.Serial).ToArray();

        Assert.HasCount(serials.Distinct().Count(), serials, "no item is in two places");
        CollectionAssert.IsSubsetOf(new[] { first.Serial, second.Serial, gear.Serial, other.Serial }, serials);

        // A full bag cannot take the gear back.
        while (fixture.Inventory.Find(StorageGroup.Common)!.FirstFree() >= 0)
        {
            fixture.Give(FirstWeapon);
        }

        Assert.AreEqual(HelperGear.NoRoom, await fixture.EquipAsync(null, gear, 1));
        Assert.AreSame(gear, fixture.Worn[1], "a refused take-off leaves the item on the helper");
    }

    /// <summary>
    /// What a helper wears adds its physical attack to both attacks and its physical defense to the defense; the orb of a
    /// friend adds its value while its day lasts, and nothing after.
    /// </summary>
    [TestMethod]
    public async Task WornItemsAndAValidOrbRaiseTheStatsOfTheHelper()
    {
        using var fixture = new Fixture();

        Dictionary<byte, float> Added() => fixture.Gear.Record(fixture.Player, fixture.World.Get<HelperComponent>(fixture.Player).Book!.Find(Mirium)!)
            .Additional.ToDictionary(e => e.Type, e => e.Value);

        Assert.IsEmpty(Added(), "a helper that wears nothing has nothing added");

        Assert.AreEqual(0, await fixture.EquipAsync(fixture.Give(FirstWeapon), null, 0));
        Assert.AreEqual(0, await fixture.EquipAsync(fixture.Give(FirstGear), null, 1));

        Assert.AreEqual(157f, Added()[20]);
        Assert.AreEqual(157f, Added()[21]);
        Assert.AreEqual(42f, Added()[24]);

        var orb = new HelperOrb { Friend = 9, SupportType = HelperStats.AttackOrb, Value = 30f, Until = fixture.Time.GetUtcNow().AddHours(24) };

        Assert.IsTrue(fixture.Gear.WearOrb(fixture.Player, Mirium, orb, out var record));
        Assert.AreEqual(9, record.Support.Friend);
        Assert.AreEqual(187f, Added()[21], "the orb adds to the attack while it lasts");

        fixture.Time.Advance(TimeSpan.FromHours(24));

        Assert.AreEqual(157f, Added()[21], "an orb whose day is over adds nothing");
        Assert.AreEqual(0, fixture.Gear.Record(fixture.Player, fixture.World.Get<HelperComponent>(fixture.Player).Book!.Find(Mirium)!).Support.Friend,
            "and is not shown");
    }
}
