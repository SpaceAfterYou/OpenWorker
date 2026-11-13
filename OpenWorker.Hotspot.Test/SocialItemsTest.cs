using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.Numerics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.SocialItems;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.SocialItems.Requests;
using OpenWorker.Hotspot.Modules.SocialItems.Responses;
using OpenWorker.Hotspot.Modules.SocialItems.Types;
using OpenWorker.UpdateContent.Res.Rows;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The social objects of a district's channel (<see cref="SocialItems"/>): putting one down, who is told about it, using it and
/// its end.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class SocialItemsTest
{
    private const int Mat = 600000001;
    private const int Radio = 600000002;
    private const short MatRow = 10010;
    private const int SocialKind = 170203;
    private const float Cell = WorldOptions.DefaultVisibilityRadius;

    private sealed class Grounds(SocialItemGround ground) : ISocialItemGrounds
    {
        public ValueTask<SocialItemGround> GetAsync() => ValueTask.FromResult(ground);
    }

    private sealed class Harness : IDisposable
    {
        private readonly InventoryFactory _bags;

        public Harness(int seats = 2)
        {
            Catalog = new ItemCatalog(
                new ReadOnlyCollection<ItemRow>(
                [
                    TestRows.Build<ItemRow>(("Id", Mat), ("Classify", SocialKind), ("EffectValue", (int)MatRow), ("UsePeriodValue", 1), ("Stack", (short)10)),
                    TestRows.Build<ItemRow>(("Id", Radio), ("Classify", SocialKind), ("EffectValue", 10020), ("UsePeriodValue", 1), ("Stack", (short)10))
                ]),
                new ReadOnlyCollection<ItemClassifyRow>(
                    [TestRows.Build<ItemClassifyRow>(("Id", SocialKind), ("StorageKind", (byte)13), ("ItemUseType", (byte)0x54))]),
                new([]),
                new([]));

            var serials = new LocalItemSerials();

            _bags = new InventoryFactory(Catalog, new([]), serials);
            Inventory = new InventoryService(District.Ecs, Catalog, serials);
            Clock = new SimulationClock(new WorldLoop(NullLogger<WorldLoop>.Instance), NullLogger<SimulationClock>.Instance, time: Time);

            // The mat is an object of a player; the radio's row is of a game for two.
            var rules = new ReadOnlyCollection<SocialItemRuleRow>(
            [
                TestRows.Build<SocialItemRuleRow>(("Id", (ushort)MatRow), ("CollisionRange", 200), ("UseMax", (byte)seats)),
                TestRows.Build<SocialItemRuleRow>(("Id", (ushort)10020), ("CollisionRange", 200), ("UseMax", (byte)seats), ("Kind", (byte)3))
            ]);

            // A square kept free around (3000, 3000), and a floor that ends at y = 9000.
            var ground = new SocialItemGround([(new Vector3(3000, 3000, 0), new Vector3(1000, 1000, 100), 90f)], point => point.Y < 9000);

            Objects = new SocialItems(District.Ecs, Inventory, Catalog, rules, new Grounds(ground), Clock, Options.Create(new WorldOptions()));
        }

        public DistrictHarness District { get; } = new();

        public FakeTimeProvider Time { get; } = new();

        public ItemCatalog Catalog { get; }

        public InventoryService Inventory { get; }

        public SimulationClock Clock { get; }

        public SocialItems Objects { get; }

        /// <summary>
        /// A player at the place with a stack of mats in its cash bag, who has asked what is around it.
        /// </summary>
        public Player Join(float x, float y, int item = Mat)
        {
            var player = District.Join(x, y);

            District.Ecs.Set(player.Entity, new PersonInfoComponent { Name = "P" + player.Actor, Hero = Hero.Haru });
            District.Ecs.Set(player.Entity, new LevelComponent(10, 0));
            District.Ecs.Set(player.Entity, new InventoryComponent(_bags.CreateNew(new PersonPersistent
            {
                Id = player.Actor, Name = "P", Hero = Hero.Haru,
                DefaultHairStyle = 0, DefaultHairColor = 0, DefaultEyeColor = 0, DefaultSkinColor = 0,
                EquippedHairStyle = 0, EquippedHairColor = 0, EquippedEyeColor = 0, EquippedSkinColor = 0,
                Location = 0, PositionX = 0, PositionY = 0, PositionZ = 0, RotationX = 0, FatiguePointCommon = 0,
                Account = null!, Gate = null!
            })));

            Assert.IsTrue(Inventory.TryGive(player.Entity, item, 3));

            Objects.Show(player.Entity);

            return player;
        }

        public ItemInstance Stack(Player player) =>
            Inventory.InventoryOf(player.Entity)!.Find(StorageGroup.Cash)!.Slots.OfType<ItemInstance>().First();

        /// <summary>
        /// The request the client sends to put down the object of the item in the player's cash bag at the place.
        /// </summary>
        public SocialItemStartRequest Start(Player player, float x, float y, int? item = null, short social = MatRow)
        {
            var stack = Stack(player);

            Span<byte> bytes = stackalloc byte[33];

            var writer = new PacketWriter(bytes);

            writer.WriteByte((byte)StorageGroup.Cash);
            writer.WriteInt16(stack.Slot);
            writer.WriteInt64(stack.Serial);
            writer.WriteInt32(item ?? stack.Item);
            writer.WriteInt16(social);
            writer.WriteSingle(x);
            writer.WriteSingle(y);
            writer.WriteSingle(0f);
            writer.WriteSingle(90f);

            var reader = new PacketReader(bytes);

            return SocialItemStartRequest.Read(ref reader);
        }

        public static SocialItemUseRequest Use(int prop, float x, float y)
        {
            Span<byte> bytes = stackalloc byte[20];

            var writer = new PacketWriter(bytes);

            writer.WriteInt32(prop);
            writer.WriteSingle(x);
            writer.WriteSingle(y);
            writer.WriteSingle(0f);
            writer.WriteSingle(0f);

            var reader = new PacketReader(bytes);

            return SocialItemUseRequest.Read(ref reader);
        }

        public void Dispose() => District.Dispose();
    }

    private static int[] Props(Player player, SocialItemOpcode command) => player.Received()
        .Where(e => e.Is(GroupOpcode.SocialItem, (byte)command))
        .Select(e => BinaryPrimitives.ReadInt32LittleEndian(e.Body.AsSpan(command == SocialItemOpcode.Infos ? 1 : 0)))
        .ToArray();

    /// <summary>
    /// An object that is put down is told to the players who see its place and to nobody else; a player who comes into sight of
    /// it later is told then, and a player who walks away is told it is gone. When its owner leaves, the object goes and those
    /// who saw it are told.
    /// </summary>
    [TestMethod]
    public async Task AnObjectIsToldToThoseWhoSeeItsPlace()
    {
        using var harness = new Harness();

        var owner = harness.Join(100, 100);
        var near = harness.Join(Cell * 1.5f, 100);
        var far = harness.Join(Cell * 4f, 100);

        await harness.District.ResetAsync(owner, near, far);

        Assert.AreEqual((ushort)0, await harness.Objects.StartAsync(owner.Entity, harness.Start(owner, 200, 100)));

        var placed = harness.Objects.OwnedBy(owner.Entity)!;

        await harness.District.SettleAsync(owner, near, far);

        CollectionAssert.AreEqual(new[] { placed.Actor }, Props(owner, SocialItemOpcode.Start), "the owner keeps the object as its own");
        CollectionAssert.AreEqual(new[] { placed.Actor }, Props(owner, SocialItemOpcode.In));
        CollectionAssert.AreEqual(new[] { placed.Actor }, Props(near, SocialItemOpcode.In));
        Assert.IsEmpty(Props(far, SocialItemOpcode.In), "a player out of sight is not told");
        Assert.AreEqual(2, harness.Stack(owner).Count, "the item is used up");

        DistrictHarness.Forget(owner, near, far);

        harness.Objects.Moved(far.Entity, new Vector3(Cell * 1.2f, 100, 0));
        harness.Objects.Moved(near.Entity, new Vector3(Cell * 5f, 100, 0));

        await harness.District.SettleAsync(owner, near, far);

        CollectionAssert.AreEqual(new[] { placed.Actor }, Props(far, SocialItemOpcode.Infos), "a player who comes into sight is told");
        CollectionAssert.AreEqual(new[] { placed.Actor }, Props(near, SocialItemOpcode.Out), "a player who walks away is told it is gone");

        DistrictHarness.Forget(owner, near, far);

        harness.Objects.Leave(owner.Entity);

        await harness.District.SettleAsync(near, far);

        CollectionAssert.AreEqual(new[] { placed.Actor }, Props(far, SocialItemOpcode.Out), "the object goes with its owner");
        Assert.IsEmpty(Props(near, SocialItemOpcode.Out), "a player who no longer saw it is not told twice");
        Assert.IsEmpty(harness.Objects.In(0));
    }

    /// <summary>
    /// The checks of putting an object down, in the order of the original: the item must be one that puts down the row named,
    /// the row must be of an object of a player, the channel must have room, and the place must be free, on the floor, near
    /// the player and off the other objects. A refusal uses nothing up. A second object of the owner takes the place of its first.
    /// </summary>
    [TestMethod]
    public async Task PuttingAnObjectDownIsChecked()
    {
        using var harness = new Harness();

        var owner = harness.Join(100, 100);
        var other = harness.Join(500, 500);
        var radioOwner = harness.Join(100, 100, Radio);

        Assert.AreEqual(SocialItemCodes.Failed, await harness.Objects.StartAsync(owner.Entity, harness.Start(owner, 200, 100, item: Radio)),
            "the item named is not the one in the slot");
        Assert.AreEqual(SocialItemCodes.Failed, await harness.Objects.StartAsync(owner.Entity, harness.Start(owner, 200, 100, social: 10020)),
            "the item does not put that row down");
        Assert.AreEqual(SocialItemCodes.Failed, await harness.Objects.StartAsync(radioOwner.Entity, harness.Start(radioOwner, 200, 100, social: 10020)),
            "a game for two is not put down");
        Assert.AreEqual(SocialItemCodes.BadPlace, await harness.Objects.StartAsync(owner.Entity, harness.Start(owner, 1500, 100)),
            "too far from the player");
        Assert.AreEqual(SocialItemCodes.BadPlace, await harness.Objects.StartAsync(owner.Entity, harness.Start(owner, float.NaN, 100)));

        harness.District.Ecs.Set(owner.Entity, harness.District.Ecs.Get<Gameplay.Messages.Response.Person.WorldComponent>(owner.Entity) with
        {
            Position = new Vector3(3000, 2900, 0)
        });

        Assert.AreEqual(SocialItemCodes.BadPlace, await harness.Objects.StartAsync(owner.Entity, harness.Start(owner, 3000, 3000)),
            "a box kept free");

        harness.District.Ecs.Set(owner.Entity, harness.District.Ecs.Get<Gameplay.Messages.Response.Person.WorldComponent>(owner.Entity) with
        {
            Position = new Vector3(100, 8900, 0)
        });

        Assert.AreEqual(SocialItemCodes.BadPlace, await harness.Objects.StartAsync(owner.Entity, harness.Start(owner, 100, 8950)),
            "no floor at the edge of the object");
        Assert.AreEqual(3, harness.Stack(owner).Count, "a refusal uses nothing up");

        harness.District.Ecs.Set(owner.Entity, harness.District.Ecs.Get<Gameplay.Messages.Response.Person.WorldComponent>(owner.Entity) with
        {
            Position = new Vector3(100, 100, 0)
        });

        Assert.AreEqual((ushort)0, await harness.Objects.StartAsync(owner.Entity, harness.Start(owner, 200, 100)));
        Assert.AreEqual(SocialItemCodes.BadPlace, await harness.Objects.StartAsync(other.Entity, harness.Start(other, 300, 150)),
            "on another object");

        var first = harness.Objects.OwnedBy(owner.Entity)!;

        Assert.AreEqual((ushort)0, await harness.Objects.StartAsync(owner.Entity, harness.Start(owner, 600, 100)));
        Assert.AreNotEqual(first.Actor, harness.Objects.OwnedBy(owner.Entity)!.Actor);
        Assert.HasCount(1, harness.Objects.In(0), "the second object of the owner takes the place of the first");

        for (var i = 1; i < SocialItems.MaxPerChannel; i++)
        {
            var filler = harness.Join(i * 400f, 4000);

            Assert.AreEqual((ushort)0, await harness.Objects.StartAsync(filler.Entity, harness.Start(filler, i * 400f, 4100)));
        }

        Assert.AreEqual(SocialItemCodes.TooMany, await harness.Objects.StartAsync(other.Entity, harness.Start(other, 500, 600)),
            "the channel is full");
    }

    /// <summary>
    /// The owner always gets on its object; the others while a seat is free, the owner's seat apart. A player on an object gets
    /// on no other until it stops, and stopping is told to those who see the player.
    /// </summary>
    [TestMethod]
    public async Task SeatsOfAnObject()
    {
        using var harness = new Harness(seats: 1);

        var owner = harness.Join(100, 100);
        var first = harness.Join(150, 100);
        var second = harness.Join(150, 150);

        Assert.AreEqual((ushort)0, await harness.Objects.StartAsync(owner.Entity, harness.Start(owner, 200, 100)));

        var prop = harness.Objects.OwnedBy(owner.Entity)!.Actor;

        Assert.AreEqual((ushort)0, harness.Objects.Use(first.Entity, Harness.Use(prop, 150, 100)));
        Assert.AreEqual(SocialItemCodes.Full, harness.Objects.Use(second.Entity, Harness.Use(prop, 150, 150)), "the one seat is taken");
        Assert.AreEqual((ushort)0, harness.Objects.Use(owner.Entity, Harness.Use(prop, 100, 100)), "the owner has a seat of its own");
        Assert.AreEqual(SocialItemCodes.Full, harness.Objects.Use(first.Entity, Harness.Use(prop, 150, 100)), "a player sits once");
        Assert.AreEqual(SocialItemCodes.Failed, harness.Objects.Use(second.Entity, Harness.Use(prop, 1500, 150)), "too far from the player");

        await harness.District.ResetAsync(owner, first, second);

        Assert.AreEqual((ushort)0, harness.Objects.Stop(first.Entity));
        Assert.AreEqual(SocialItemCodes.Unknown, harness.Objects.Stop(first.Entity), "the player uses nothing now");
        Assert.AreEqual((ushort)0, harness.Objects.Use(second.Entity, Harness.Use(prop, 150, 150)), "the seat is free again");

        await harness.District.SettleAsync(owner, first, second);

        Assert.HasCount(1, second.Received().Where(e => e.Is(GroupOpcode.SocialItem, (byte)SocialItemOpcode.Stop)), "the stop is told to those around");
        CollectionAssert.AreEqual(new[] { owner.Actor, second.Actor }, harness.Objects.OwnedBy(owner.Entity)!.Users);
    }

    /// <summary>
    /// An object lasts as long as its item says, on the clock of the district, and then goes for everybody who saw it.
    /// </summary>
    [TestMethod]
    public async Task AnObjectGoesWhenItsTimeRunsOut()
    {
        using var harness = new Harness();

        var owner = harness.Join(100, 100);

        Assert.AreEqual((ushort)0, await harness.Objects.StartAsync(owner.Entity, harness.Start(owner, 200, 100)));

        await harness.District.ResetAsync(owner);

        // One minute of the item, a tick of four periods at a time.
        for (var i = 0; i < 299; i++)
        {
            harness.Time.Advance(SimulationClock.MaxStep);
            harness.Clock.Tick();
        }

        Assert.HasCount(1, harness.Objects.In(0), "a moment before its end");

        for (var i = 0; i < 2; i++)
        {
            harness.Time.Advance(SimulationClock.MaxStep);
            harness.Clock.Tick();
        }

        Assert.IsEmpty(harness.Objects.In(0));
        Assert.IsNull(harness.Objects.OwnedBy(owner.Entity));

        await harness.District.SettleAsync(owner);

        Assert.HasCount(1, Props(owner, SocialItemOpcode.Out));
    }

    /// <summary>
    /// The bytes of an object the client reads: the actor, the owner, the item, the row, the milliseconds left as eight bytes,
    /// a one-byte count of the users and each user; the use of it adds the player before and the place after.
    /// </summary>
    [TestMethod]
    public void TheUseOfAnObjectIsWrittenAsTheClientReadsIt()
    {
        var message = new SocialItemUseResponse
        {
            Actor = 7,
            Prop = new SocialItemValue { Actor = 9, Owner = 8, Item = Mat, SocialItem = MatRow, Remaining = 60_000, Users = [8, 7] },
            Position = new Vector3(1, 2, 3),
            Rotation = 4
        };

        var expected = new byte[4 + 4 + 4 + 4 + 2 + 8 + 1 + 8 + 16];
        var at = 0;

        void Int(int value) { BinaryPrimitives.WriteInt32LittleEndian(expected.AsSpan(at), value); at += 4; }
        void Float(float value) { BinaryPrimitives.WriteSingleLittleEndian(expected.AsSpan(at), value); at += 4; }

        Int(7);
        Int(9);
        Int(8);
        Int(Mat);
        BinaryPrimitives.WriteInt16LittleEndian(expected.AsSpan(at), MatRow);
        at += 2;
        BinaryPrimitives.WriteInt64LittleEndian(expected.AsSpan(at), 60_000);
        at += 8;
        expected[at++] = 2;
        Int(8);
        Int(7);
        Float(1);
        Float(2);
        Float(3);
        Float(4);

        CollectionAssert.AreEqual(expected, WireBytes.Of(message));
    }
}
