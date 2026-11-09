using System.Buffers.Binary;
using System.Text;
using Arch.Core;
using OpenWorker.DistrictServer.Server;
using OpenWorker.Gameplay.Combat;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Helpers;
using OpenWorker.Gameplay.Modules.Helpers.Components;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Helpers.Requests;
using OpenWorker.Hotspot.Modules.Helpers.Responses;
using OpenWorker.Hotspot.Modules.Helpers.Types;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.MazeServer.Services;
using OpenWorker.Server.Common.Services;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class HelperTest
{
    // The size of an item slot with nothing in it, and of a helper with nothing worn and no stats.
    private const int NoItem = 82;
    private const int BareHelper = 4 + 3 * NoItem + 18 + 2 + 2 + 1;

    private static byte[] Written(IWritableData message)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        message.Write(writer);
        writer.Flush();

        return stream.ToArray();
    }

    private static byte[] Written(IResponseHotspotMessage message) => Written((IWritableData)message);

    // Reads a helper in the order the client does and returns what it found, leaving the reader after the helper.
    private static (int Helper, int Items, byte Order, int Origin, int Additional, bool Summoned) ReadAsClient(BinaryReader reader)
    {
        var helper = reader.ReadInt32();
        var items = 0;

        for (var slot = 0; slot < 3; slot++)
        {
            var item = reader.ReadInt32();
            reader.ReadInt64();
            reader.ReadInt16();
            reader.ReadByte();

            for (var option = 0; option < 5; option++)
            {
                reader.ReadInt16();
                reader.ReadInt32();
            }

            reader.ReadBytes(3);
            reader.ReadInt64();
            reader.ReadBytes(3);
            reader.ReadInt32();

            var broach = reader.ReadInt16();
            Assert.IsTrue(broach is >= 1 and <= 15, "the broach state of an item has 1 to 15 characters");
            reader.ReadBytes(broach);
            reader.ReadBytes(2);

            if (item != -1)
            {
                items++;
            }
        }

        reader.ReadInt32();
        reader.ReadByte();
        reader.ReadSingle();
        reader.ReadInt64();
        var summoned = reader.ReadByte() != 0;

        var origin = reader.ReadInt16();
        reader.ReadBytes(origin * 5);
        var additional = reader.ReadInt16();
        reader.ReadBytes(additional * 5);

        return (helper, items, reader.ReadByte(), origin, additional, summoned);
    }

    private static HelperValue Helper(int id, byte order = 0, bool summoned = false, params HelperStatValue[] stats) => new()
    {
        Helper = id,
        Order = order,
        Summoned = summoned,
        Origin = stats,
        Additional = []
    };

    [TestMethod]
    public void HelperWithoutItemsOrStatsHasTheSizeTheClientExpects()
    {
        var bytes = Written(Helper(10105162));

        Assert.HasCount(BareHelper, bytes);

        using var reader = new BinaryReader(new MemoryStream(bytes));
        var read = ReadAsClient(reader);

        Assert.AreEqual(10105162, read.Helper);
        Assert.AreEqual(0, read.Items);
        Assert.AreEqual(bytes.Length, reader.BaseStream.Position);
    }

    [TestMethod]
    public void WornItemIsWrittenInItsSlotAsTheInventoryWritesIt()
    {
        var gear = new ItemValue
        {
            Item = 360010202,
            Serial = new SerialValue(1234567890123),
            Count = 1,
            BindType = (ItemBindType)1,
            OptionList = [new ItemOption(3, 86), new ItemOption(0, 0), new ItemOption(0, 0), new ItemOption(0, 0), new ItemOption(0, 0)],
            Upgrade = 2,
            Endurance = 100,
            DueTo = DateTimeOffset.UnixEpoch,
            BroachState = "000000000000000"
        };

        var bytes = Written(new HelperValue
        {
            Helper = 10105166,
            Items = [HelperValue.NoItem, gear],
            Origin = [new HelperStatValue(21, 300f)],
            Additional = [],
            Order = 1
        });

        using var stream = new MemoryStream();
        using var expected = new BinaryWriter(stream);

        void Empty()
        {
            expected.Write(-1);
            expected.Write(-1L);
            expected.Write(new byte[2 + 1 + 5 * 6 + 3 + 8 + 3 + 4]);
            expected.Write((short)15);
            expected.Write(Encoding.ASCII.GetBytes("000000000000000"));
            expected.Write(new byte[2]);
        }

        expected.Write(10105166);
        Empty();

        // The gear: item, serial, count, binding, five options, upgrade, endurance, sockets, date, upgrade count and
        // limit, flag, experience, the broaches, restores and seals.
        expected.Write(360010202);
        expected.Write(1234567890123L);
        expected.Write((short)1);
        expected.Write((byte)1);
        expected.Write((short)3);
        expected.Write(86);
        expected.Write(new byte[4 * 6]);
        expected.Write((byte)2);
        expected.Write((byte)100);
        expected.Write((byte)0);
        expected.Write(0L);
        expected.Write(new byte[3]);
        expected.Write(0);
        expected.Write((short)15);
        expected.Write(Encoding.ASCII.GetBytes("000000000000000"));
        expected.Write(new byte[2]);

        Empty();

        // No orb, not called, one stat of its own and none added, the second place.
        expected.Write(new byte[4 + 1 + 4 + 8 + 1]);
        expected.Write((short)1);
        expected.Write((byte)21);
        expected.Write(300f);
        expected.Write((short)0);
        expected.Write((byte)1);
        expected.Flush();

        CollectionAssert.AreEqual(stream.ToArray(), bytes);

        using var reader = new BinaryReader(new MemoryStream(bytes));
        Assert.AreEqual(1, ReadAsClient(reader).Items);
        Assert.AreEqual(bytes.Length, reader.BaseStream.Position);
    }

    [TestMethod]
    public void StatsAndOrderComeAfterTheItems()
    {
        var bytes = Written(Helper(10105166, 3, true, new HelperStatValue(2, 1.5f), new HelperStatValue(7, 40f)));

        Assert.HasCount(BareHelper + 2 * 5, bytes);

        using var reader = new BinaryReader(new MemoryStream(bytes));
        var read = ReadAsClient(reader);

        Assert.AreEqual((byte)3, read.Order);
        Assert.AreEqual(2, read.Origin);
        Assert.AreEqual(0, read.Additional);
        Assert.IsTrue(read.Summoned);
        Assert.AreEqual(bytes.Length, reader.BaseStream.Position);
    }

    [TestMethod]
    public void ListIsPersonThenHelpersThenTheAutoSummonFlag()
    {
        var bytes = Written(new HelperListLoadResponse { Person = 7, Helpers = [Helper(1), Helper(2, 1)], AutoSummon = 1 });

        Assert.HasCount(4 + 2 + 2 * BareHelper + 1, bytes);
        Assert.AreEqual(7, BitConverter.ToInt32(bytes));
        Assert.AreEqual((short)2, BitConverter.ToInt16(bytes, 4));
        Assert.AreEqual((byte)1, bytes[^1]);
    }

    [TestMethod]
    public void EmptyListStillHasThePersonAndTheFlag()
    {
        CollectionAssert.AreEqual(new byte[] { 7, 0, 0, 0, 0, 0, 0 }, Written(new HelperListLoadResponse { Person = 7, Helpers = [], AutoSummon = 0 }));
    }

    [TestMethod]
    public void MessagesThatCarryAHelper()
    {
        Assert.HasCount(4 + 4 + 1 + BareHelper, Written(new HelperSummonResponse { Person = 1, Actor = 2, Summon = 1, Helper = Helper(3) }));
        Assert.HasCount(BareHelper, Written(new HelperAddResponse { Helper = Helper(3) }));
        Assert.HasCount(BareHelper, Written(new HelperUpdateInfoResponse { Helper = Helper(3) }));
        Assert.HasCount(4 + BareHelper, Written(new HelperEquipResponse { Result = 0, Helper = Helper(3) }));
        Assert.HasCount(4 + 4 + BareHelper, Written(new HelperSupportEquipResponse { Result = 0, Helper = 3, Record = Helper(3) }));
    }

    [TestMethod]
    public void SupportMessages()
    {
        var info = new HelperSupportInfoValue(5, 2, 0.5f, DateTimeOffset.FromUnixTimeSeconds(1000), 1, true);

        var bytes = Written(new HelperSupportInfoResponse { Info = info });

        Assert.HasCount(4 + 1 + 4 + 8 + 1 + 1, bytes);
        Assert.AreEqual(5, BitConverter.ToInt32(bytes));
        Assert.AreEqual(1000L, BitConverter.ToInt64(bytes, 9));
        Assert.HasCount(4 + 19, Written(new HelperSupportRegisterResponse { Result = 0, Info = info }));
        Assert.HasCount(4, Written(new HelperSupportRewardResponse { Result = 0 }));
    }

    [TestMethod]
    public void SupportListIsACountAndFriends()
    {
        CollectionAssert.AreEqual(new byte[] { 0, 0 }, Written(new HelperSupportListResponse { Friends = [] }));

        var friend = new HelperSupportFriendValue(5, 1, 0.5f, DateTimeOffset.FromUnixTimeSeconds(1), 0.25f, "Ab", 40, 2);
        var bytes = Written(new HelperSupportListResponse { Friends = [friend] });

        Assert.HasCount(2 + 4 + 1 + 4 + 8 + 4 + (2 + 2 * 2 + 1) + 1 + 1, bytes);
    }

    [TestMethod]
    public void OrderAutoSummonAndPositionHaveTheirLayouts()
    {
        var order = Written(new HelperChangeOrderResponse { Person = 1, First = 2, FirstOrder = 3, Second = 4, SecondOrder = 5, Error = 0 });

        Assert.HasCount(18, order);
        Assert.AreEqual(2, BitConverter.ToInt32(order, 4));
        Assert.AreEqual((byte)3, order[8]);
        Assert.AreEqual(4, BitConverter.ToInt32(order, 9));
        Assert.AreEqual((byte)5, order[13]);

        CollectionAssert.AreEqual(BitConverter.GetBytes(9).Append((byte)1).ToArray(), Written(new HelperAutoSummonResponse { Person = 9, Flag = 1 }));
        Assert.HasCount(16, Written(new HelperSyncPosResponse { Actor = 1, X = 2, Y = 3, Z = 4 }));
    }

    [TestMethod]
    public void StatUpdateIsACountAndTheStatsOfEveryHelper()
    {
        var bytes = Written(new HelperUserStatUpdateResponse
        {
            Helpers = [new HelperStatUpdateValue(10, [new HelperStatValue(1, 2f), new HelperStatValue(3, 4f)])]
        });

        Assert.HasCount(2 + 4 + 2 + 2 * 5, bytes);
    }

    [TestMethod]
    public void RequestsAreReadLikeTheClientWritesThem()
    {
        using var summon = new BinaryReader(new MemoryStream([1, 0x5A, 0, 0, 0]));
        var call = new HelperSummonRequest(summon);

        Assert.AreEqual((byte)1, call.Summon);
        Assert.AreEqual(90, call.Helper);
        Assert.AreEqual(5, summon.BaseStream.Position);

        using var order = new BinaryReader(new MemoryStream(new byte[18]));
        Assert.AreEqual(0, new HelperChangeOrderRequest(order).Error);
        Assert.AreEqual(18, order.BaseStream.Position);

        using var equip = new BinaryReader(new MemoryStream(new byte[1 + 4 + 2 + 8 + 4 + 4 + 2 + 8]));
        _ = new HelperEquipRequest(equip);
        Assert.AreEqual(33, equip.BaseStream.Position);

        using var support = new BinaryReader(new MemoryStream(new byte[12]));
        _ = new HelperSupportEquipRequest(support);
        Assert.AreEqual(12, support.BaseStream.Position);

        using var auto = new BinaryReader(new MemoryStream([9, 0, 0, 0, 1]));
        var flag = new HelperAutoSummonRequest(auto);
        Assert.AreEqual(9, flag.Person);
        Assert.AreEqual((byte)1, flag.Flag);
    }

    /// <summary>
    /// The window of the helpers is answered in a district and in a maze: every request it sends has its handler in the
    /// assembly both servers load. A call outside a maze is refused as the original refuses it, a call in a maze as one
    /// the maze does not allow, as long as helpers are not called into mazes.
    /// </summary>
    [TestMethod]
    [DoNotParallelize]
    public async Task DistrictAndMazeBothAnswerTheWindowOfTheHelpers()
    {
        var shared = typeof(HelperService).Assembly;
        var requests = typeof(HelperSummonRequest).Assembly.GetTypes()
            .Where(e => e.Namespace == typeof(HelperSummonRequest).Namespace && typeof(IRequestHotspotMessage).IsAssignableFrom(e))
            .ToArray();
        var handlers = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(e => e.GetTypes())
            .Where(e => e is { IsClass: true, IsAbstract: false })
            .SelectMany(e => e.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHotspotHandler<>))
                .Select(i => (Request: i.GetGenericArguments()[0], Handler: e)))
            .ToArray();

        Assert.HasCount(10, requests);

        foreach (var request in requests)
        {
            var handler = handlers.Where(e => e.Request == request).Select(e => e.Handler).ToArray();

            Assert.HasCount(1, handler, request.Name);
            Assert.AreEqual(shared, handler[0].Assembly, request.Name);
        }

        foreach (var server in new[] { typeof(ChannelAudience).Assembly, typeof(MazeAudience).Assembly })
        {
            Assert.IsTrue(server.GetReferencedAssemblies().Any(e => e.Name == shared.GetName().Name), server.GetName().Name);
        }

        using var world = World.Create();
        var service = new HelperService(new HelperManager(world, null!, HelperCatalog.From([10105166])), null!, null!);

        async Task<ushort> Refusal(bool inMaze, byte summon)
        {
            var connection = new RecordingConnection();
            var player = world.Create(new ServerSessionComponent(connection), new HelperComponent { Person = 1, Book = new HelperBook(), InMaze = inMaze });

            using var request = new BinaryReader(new MemoryStream([summon, .. BitConverter.GetBytes(10105166)]));
            await service.OnHandleAsync(new ServiceHandleContext(player, CancellationToken.None), new HelperSummonRequest(request));
            await MetricAssertions.UntilAsync(() => connection.Frames.Length > 0, "the client is answered");

            var frame = PlayerFrame.Open(connection.Frames[0]);
            Assert.IsTrue(frame.Is(GroupOpcode.Helper, (byte)HelperOpcode.Summon | 0x80));

            return BinaryPrimitives.ReadUInt16LittleEndian(frame.Body);
        }

        Assert.AreEqual(HelperManager.NotInMaze, await Refusal(false, 1));
        Assert.AreEqual(HelperManager.NotInMaze, await Refusal(false, 0));
        Assert.AreEqual(HelperManager.CannotSummon, await Refusal(true, 1));
        Assert.AreEqual(HelperManager.CannotSummon, await Refusal(true, 0));
        Assert.IsTrue(new HelperBook().AutoSummon, "a new person has the automatic call on");
    }

    // A status row with the columns named set and every other column zero.
    private static StatusRow Status(params (int Column, float Value)[] values)
    {
        const int floatsBeforeResists = 35, resists = 12, floatsAfterResists = 8;
        var map = values.ToDictionary(e => e.Column, e => e.Value);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write((short)3001);

        for (var column = 1; column <= floatsBeforeResists; column++)
        {
            writer.Write(map.GetValueOrDefault(column));
        }

        writer.Write(new byte[resists]);

        for (var column = 0; column < floatsAfterResists; column++)
        {
            writer.Write(map.GetValueOrDefault(floatsBeforeResists + resists + 1 + column));
        }

        writer.Flush();
        stream.Position = 0;

        return new StatusRow(new BinaryReader(stream));
    }

    /// <summary>
    /// What a helper has by itself grows with the level of its owner and carries the additions of its monster row, under
    /// the stat numbers the client keeps; what it wears and a valid orb add to the two attacks and the defense.
    /// </summary>
    [TestMethod]
    public void HelperStatsFollowTheOwnerAndWhatTheHelperWears()
    {
        // Strength 10 + 2 a level, vitality 5, 10 health a point of vitality, 100 health to start with.
        var status = Status((1, 10f), (2, 2f), (9, 5f), (13, 10f), (18, 1f), (51, 100f));

        static Dictionary<byte, float> Read(HelperStatValue[] stats) => stats.ToDictionary(e => e.Type, e => e.Value);

        var first = Read(HelperStats.Origin(status, 0, default, 1));
        var eleventh = Read(HelperStats.Origin(status, 0, default, 11));
        var added = Read(HelperStats.Origin(status, 0, new StatBonuses { Health = 500f, PhysicalAttack = 50f }, 1));

        Assert.AreEqual(100f, first[21]);
        Assert.AreEqual(80f, first[20]);
        Assert.AreEqual(300f, eleventh[21], "the level of the owner raises the strength and the attack");
        Assert.AreEqual(150f, first[10]);
        Assert.AreEqual(650f, added[10], "the health of the monster row is added");
        Assert.AreEqual(150f, added[21], "the attack of the monster row is added");
        Assert.IsTrue(first.Keys.All(e => e < 61), "the client has room for 61 stats");

        var now = DateTimeOffset.FromUnixTimeSeconds(1_000_000);
        var orb = new HelperSupportInfoValue(7, HelperStats.AttackOrb, 30f, now.AddHours(1), 0, true);

        var worn = Read(HelperStats.Additional([(157, 42)], orb, now));
        var expired = Read(HelperStats.Additional([(157, 42)], orb with { Date = now.AddSeconds(-1) }, now));

        CollectionAssert.AreEquivalent(new byte[] { 20, 21, 24 }, worn.Keys.ToArray());
        Assert.AreEqual(187f, worn[20]);
        Assert.AreEqual(187f, worn[21]);
        Assert.AreEqual(42f, worn[24]);
        Assert.AreEqual(157f, expired[21], "an orb whose date is past adds nothing");
        Assert.IsEmpty(HelperStats.Additional([], default, now), "nothing worn adds nothing");
    }

    [TestMethod]
    public void NewHelperGoesToTheEndOfTheList()
    {
        var book = new HelperBook();

        Assert.IsTrue(book.TryAdd(10, out var first));
        Assert.IsTrue(book.TryAdd(20, out var second));
        Assert.IsFalse(book.TryAdd(10, out _));

        Assert.AreEqual((byte)0, first.Order);
        Assert.AreEqual((byte)1, second.Order);
    }

    [TestMethod]
    public void HelpersSwapTheirPlaces()
    {
        var book = new HelperBook();
        book.TryAdd(10, out _);
        book.TryAdd(20, out _);
        book.TryAdd(30, out _);

        Assert.IsTrue(book.TrySwap(10, 30, out var one, out var two));
        Assert.AreEqual((byte)2, one.Order);
        Assert.AreEqual((byte)0, two.Order);

        Assert.IsFalse(book.TrySwap(10, 99, out _, out _));
        Assert.IsFalse(book.TrySwap(10, 10, out _, out _));
    }

    [TestMethod]
    public void UnknownHelpersAreDroppedAndPlacesRenumbered()
    {
        var book = new HelperBook
        {
            Helpers = [new HelperEntry { Helper = 10, Order = 5 }, new HelperEntry { Helper = 99, Order = 1 }, new HelperEntry { Helper = 20, Order = 9 }]
        };

        book.Normalize(HelperCatalog.From([10, 20]));

        CollectionAssert.AreEqual(new[] { 10, 20 }, book.Helpers.Select(e => e.Helper).ToArray());
        CollectionAssert.AreEqual(new byte[] { 0, 1 }, book.Helpers.Select(e => e.Order).ToArray());
    }

    [TestMethod]
    public void TableOfTheClientHasEightHelpers()
    {
        var catalog = new HelperCatalog(TestTables.Load<HelperRow>("tb_Helper"));

        Assert.AreEqual(8, catalog.All.Count());
        Assert.IsTrue(catalog.Contains(10105162));
        Assert.IsFalse(catalog.Contains(1));
    }

    [TestMethod]
    public void NameOfAFriendIsTheSameKindOfStringAsElsewhere()
    {
        // Two bytes of length, then the characters and a zero.
        var bytes = Written(new HelperSupportFriendValue(0, 0, 0, DateTimeOffset.UnixEpoch, 0, "A", 0, 0));

        Assert.AreEqual((short)3, BitConverter.ToInt16(bytes, 4 + 1 + 4 + 8 + 4));
        Assert.AreEqual("A", Encoding.Unicode.GetString(bytes, 4 + 1 + 4 + 8 + 4 + 2, 2));
    }
}
