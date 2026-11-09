using System.Buffers.Binary;
using System.Collections.ObjectModel;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Drops;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Requests;
using OpenWorker.MazeServer.Services;
using OpenWorker.Res.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The key reward of the window at the end of a maze, through the handler of the maze, against the item tables of the client:
/// set OPENWORKER_TEST_RES to the folder with the *.res files. The reward table of the server and the box are made here.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MazeKeyRewardTest
{
    private const int Maze = 21311;
    private const int Box = 900001;
    private const short RewardCount = 3;

    // Where the window carries the item of the key reward: after the rank, the points, the difficulty, nine numbers and the
    // base item with its count, then the count and the item.
    private const int KeyRewardCountOffset = 1 + 4 + 4 + 2 + 9 * 4 + 2 + 4;

    private static ReadOnlyCollection<T> Rows<T>(Action<BinaryWriter> write) where T : struct
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        write(writer);
        stream.Position = 0;

        using var reader = new BinaryReader(stream);

        return new ReadOnlyCollection<T>([(T)Activator.CreateInstance(typeof(T), reader)!]);
    }

    private static ReadOnlyCollection<MazeRewardItemRow> Reward(byte keys) => Rows<MazeRewardItemRow>(writer =>
    {
        writer.Write((uint)Maze);
        writer.Write(new byte[2 + 4 + MazeRewardItemRow.RankCount * (4 + 2 + 4 + 4)]);
        writer.Write((uint)Box);
        writer.Write(0u);
        writer.Write(keys);
        writer.Write((byte)0);
    });

    /// <summary>A box that holds one item: some gold with a rate, which the key reward leaves out, and the item.</summary>
    private static ReadOnlyCollection<ItemRandomBoxRow> OneItemBox(int item) => Rows<ItemRandomBoxRow>(writer =>
    {
        writer.Write(Box);
        writer.Write((byte)1);
        writer.Write(10000);
        writer.Write(500);
        writer.Write((short)5000);
        writer.Write((short)0);
        writer.Write((short)0);

        for (var i = 0; i < 15; i++)
        {
            writer.Write(i == 0 ? (short)5000 : (short)0);
        }

        writer.Write(0);
        writer.Write(0);

        for (var i = 0; i < 15; i++)
        {
            writer.Write(i == 0 ? item : 0);
        }

        for (var i = 0; i < 15; i++)
        {
            writer.Write(i == 0 ? (int)RewardCount : 0);
        }
    });

    private sealed class Fixture : IDisposable
    {
        private readonly RecordingConnection _connection = new();

        public World World { get; } = World.Create();
        public InventoryService Inventory { get; }
        public MazeResults Results { get; }
        public MazeRewardService Handler { get; }
        public Entity Player { get; }
        public PersonDirty Dirty { get; } = new(7);
        public int Key { get; }
        public int Item { get; }

        public Fixture(byte keys = 1)
        {
            var items = TestTables.Load<ItemRow>("tb_Item");
            var classifies = TestTables.Load<ItemClassifyRow>("tb_Item_Classify");
            var catalog = new ItemCatalog(items, classifies, TestTables.Load<InvenSlotExtendRow>("tb_Inven_Slot_Extend"), TestTables.Load<BankSlotExtendRow>("tb_Bank_Slot_Extend"));
            var serials = new LocalItemSerials();

            Inventory = new InventoryService(World, catalog, serials);

            Player = World.Create(
                Component.GetComponentType(typeof(ActorComponent)),
                Component.GetComponentType(typeof(InventoryComponent)),
                Component.GetComponentType(typeof(PersonInfoComponent)),
                Component.GetComponentType(typeof(CurrencyComponent)),
                Component.GetComponentType(typeof(ServerSessionComponent)));

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

            var bag = new InventoryFactory(catalog, TestTables.Load<CreateClothRow>("tb_Create_Cloth"), serials).CreateNew(person);

            bag.Dirty = Dirty;

            // The key is the item of its kind; the reward is a stack of the common bag the new person does not have.
            Key = items.First(e => catalog.Classify(e.Id)?.ItemUseType == ItemUseType.REWARD_ADD).Id;
            Item = items.First(e => e.Id != Key && e.Stack >= 10 && catalog.HomeOf(e.Id) == StorageGroup.Common && bag.Items.All(x => x.Item != e.Id)).Id;

            var rewards = new MazeKeyRewards(Inventory, Reward(keys), OneItemBox(Item), NullLogger<MazeKeyRewards>.Instance);

            Results = new MazeResults(World, rewards);
            Handler = new MazeRewardService(World, rewards);

            World.Set(Player, new ActorComponent(7, ActorType.User));
            World.Set(Player, new InventoryComponent(bag));
            World.Set(Player, new PersonInfoComponent { Name = "Test", Hero = Hero.Haru });
            World.Set(Player, new CurrencyComponent());
            World.Set(Player, new ServerSessionComponent(_connection));
        }

        public long Count(int item) => Inventory.CountOf(Player, item);

        /// <summary>The maze is finished: the window is shown with what it offers.</summary>
        public void FinishMaze()
        {
            Results.Start(Player, Maze);
            Results.Complete(Player);
        }

        /// <summary>The player takes the key reward with the keys of the first slot that holds them, or of the first slot of the bag.</summary>
        public ValueTask TakeAsync()
        {
            var key = Inventory.InventoryOf(Player)!.Items.FirstOrDefault(e => e.Item == Key);
            var slot = new byte[3];

            slot[0] = (byte)(key?.Storage ?? StorageGroup.Common);
            BinaryPrimitives.WriteInt16LittleEndian(slot.AsSpan(1), (short)(key?.Slot ?? 0));

            using var reader = new BinaryReader(new MemoryStream(slot));

            return Handler.OnHandleAsync(new ServiceHandleContext(Player, CancellationToken.None), new ItemMazeRewardItemRequest(reader));
        }

        public async Task<PlayerFrame> FirstAsync(GroupOpcode group, byte command)
        {
            await MetricAssertions.UntilAsync(() => Received().Any(e => e.Is(group, command)), $"The player gets {group}/{command}.");

            return Received().First(e => e.Is(group, command));
        }

        public async Task<ushort[]> ErrorsAsync(int count)
        {
            const byte refused = (byte)ItemOpcode.MazeRewardItem | 0x80;

            await MetricAssertions.UntilAsync(() => Received().Count(e => e.Is(GroupOpcode.Item, refused)) >= count, "The player is told the refusals.");

            return Received().Where(e => e.Is(GroupOpcode.Item, refused)).Select(e => BinaryPrimitives.ReadUInt16LittleEndian(e.Body)).ToArray();
        }

        private PlayerFrame[] Received() => _connection.Frames.Select(PlayerFrame.Open).ToArray();

        public void Dispose()
        {
            World.Dispose();
        }
    }

    [TestMethod]
    public async Task KeyRewardOfAFinishedMazeIsGivenOnceForItsKey()
    {
        using var f = new Fixture();

        Assert.IsTrue(f.Inventory.TryGive(f.Player, f.Key, 2));

        f.FinishMaze();

        var window = await f.FirstAsync(GroupOpcode.Maze, (byte)MazeOpcode.eSUB_CMD_MAZE_REWARD_INFO);

        Assert.AreEqual(RewardCount, BinaryPrimitives.ReadInt16LittleEndian(window.Body.AsSpan(KeyRewardCountOffset)));
        Assert.AreEqual(f.Item, BinaryPrimitives.ReadInt32LittleEndian(window.Body.AsSpan(KeyRewardCountOffset + 2)));

        await f.TakeAsync();

        Assert.AreEqual(1, f.Count(f.Key), "One key is spent");
        Assert.AreEqual(RewardCount, f.Count(f.Item), "The item of the box is given, without the gold of the box");
        Assert.IsTrue(f.Dirty.Sections.HasFlag(PersonSections.Inventory), "The keys and the item are written by the flush");

        await f.TakeAsync();

        Assert.AreEqual(1, f.Count(f.Key), "A second request spends nothing");
        Assert.AreEqual(RewardCount, f.Count(f.Item), "A second request gives nothing");
        CollectionAssert.AreEqual(new[] { MazeKeyRewards.Refused }, await f.ErrorsAsync(1));
    }

    [TestMethod]
    public async Task KeyRewardWaitsUntilThePlayerHasTheKeys()
    {
        using var f = new Fixture(keys: 2);

        Assert.IsTrue(f.Inventory.TryGive(f.Player, f.Key, 1));

        f.FinishMaze();

        await f.TakeAsync();

        Assert.AreEqual(1, f.Count(f.Key), "One key of two is not enough and is kept");
        Assert.AreEqual(0, f.Count(f.Item));

        Assert.IsTrue(f.Inventory.TryGive(f.Player, f.Key, 1));

        await f.TakeAsync();

        Assert.AreEqual(0, f.Count(f.Key));
        Assert.AreEqual(RewardCount, f.Count(f.Item), "The reward has waited for the keys");
        CollectionAssert.AreEqual(new[] { MazeKeyRewards.Refused }, await f.ErrorsAsync(1));
    }
}
