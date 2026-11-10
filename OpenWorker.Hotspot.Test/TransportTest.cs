using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.DistrictServer.Server;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Gameplay.Modules.Npcs;
using OpenWorker.Gameplay.Modules.Npcs.Components;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Movement.Requests;
using OpenWorker.MazeServer.Services;
using OpenWorker.UpdateContent.Res.Rows;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A ride at an NPC of transports, through the handler of the maze server, between two players of one channel whose
/// connections record what they get, on a clock the test moves by hand.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class TransportTest
{
    private const int NpcRow = 900;
    private const int NpcActor = 31;
    private const short Ride = 101;
    private const short OtherRide = 201;
    private const int Fare = 1000;
    private const int Seconds = 60;
    private const int Box = 5902;

    private static readonly Vector3 Station = new(68183f, 46679f, 15574f);

    private static ReadOnlyCollection<T> Rows<T>(params T[] rows) => new(rows);

    /// <summary>
    /// One NPC of transports stands where the test says, under the actor <see cref="NpcActor"/>.
    /// </summary>
    private sealed class OneNpc(Vector3 position) : INpcPlaces
    {
        public bool TryFind(Entity player, int actor, out int table, out Vector3 place)
        {
            table = NpcRow;
            place = position;

            return actor == NpcActor;
        }
    }

    /// <summary>
    /// A map with one box to end a ride at, and a note of who was told nothing may hurt them.
    /// </summary>
    private sealed class Map(int box) : ITransportMap
    {
        public List<(Entity Player, TimeSpan Length)> Protected { get; } = [];

        public bool TryFindStop(Entity player, int asked, out Vector3 position, out float rotation)
        {
            position = Station;
            rotation = 90f;

            return asked == box;
        }

        public void Protect(Entity player, TimeSpan length) => Protected.Add((player, length));
    }

    private sealed class Ferry : IDisposable
    {
        public Ferry(int boxOnTheMap = Box)
        {
            var npc = TestRows.Build<NPCRow>(("Id", NpcRow), ("FunctionType1", (byte)8), ("Function1", (short)1));
            var offers = TestRows.Build<DistrictTransportRow>(("Id", (short)1), ("Transport1", Ride), ("Transport2", (short)102));
            var rides = Rows(
                TestRows.Build<TransportInfoRow>(("Id", Ride), ("NeedGold", Fare), ("PathFile", "Scripts/Transport/Test"), ("PositionBox", (short)Box), ("RideSeconds", (short)Seconds)),
                TestRows.Build<TransportInfoRow>(("Id", OtherRide), ("NeedGold", Fare), ("PathFile", "Scripts/Transport/Test"), ("PositionBox", (short)Box), ("RideSeconds", (short)Seconds)));

            Clock = TestMazes.Clock(Time);
            Map = new Map(boxOnTheMap);

            var catalog = new ItemCatalog(Rows<ItemRow>(), Rows<ItemClassifyRow>(), Rows<InvenSlotExtendRow>(), Rows<BankSlotExtendRow>());
            var inventory = new InventoryService(District.Ecs, catalog, new LocalItemSerials());
            var credit = new NpcCreditManager(
                District.Ecs,
                new PersonStateStore(null!),
                new NpcCreditTables(Rows<CustomerGradeRow>(), Rows<CustomerBenefitRow>(), Rows<CreditRaiseRow>()),
                inventory,
                Time,
                NullLogger<NpcCreditManager>.Instance);

            Inventory = inventory;
            Service = new TransportService(
                District.Ecs,
                new ServiceNpcs(District.Ecs, new NpcCatalog(Rows(npc)), new OneNpc(Vector3.Zero)),
                new TransportTables(Rows(offers), rides),
                Map,
                inventory,
                credit,
                new ChannelAudience(District.Ecs, District.Channels),
                Clock,
                District.Errors,
                NullLogger<TransportService>.Instance);
        }

        public DistrictHarness District { get; } = new();

        public FakeTimeProvider Time { get; } = new();

        public SimulationClock Clock { get; }

        public Map Map { get; }

        public InventoryService Inventory { get; }

        public TransportService Service { get; }

        /// <summary>
        /// A player near the NPC with the gold, whose credit is loaded already (the maze loads it at the first ride).
        /// </summary>
        public Player Join(long gold, float x = 0f)
        {
            var player = District.Join(x, 0f);

            District.Ecs.Set(player.Entity, new CurrencyComponent { Gold = gold });
            District.Ecs.Set(player.Entity, new NpcCreditComponent { Person = player.Actor, Book = new NpcCreditBook() });

            return player;
        }

        public ValueTask Ask(Player rider, short row, int npc = NpcActor)
        {
            Span<byte> bytes = stackalloc byte[4 + 2 + 4];

            BinaryPrimitives.WriteInt32LittleEndian(bytes, rider.Actor);
            BinaryPrimitives.WriteInt16LittleEndian(bytes[4..], row);
            BinaryPrimitives.WriteInt32LittleEndian(bytes[6..], npc);

            var reader = new PacketReader(bytes);

            return Service.OnHandleAsync(new ServiceHandleContext(rider.Entity, CancellationToken.None), MovementTransportTakeRequest.Read(ref reader));
        }

        /// <summary>
        /// The clock runs for the time, step by step as the world loop does.
        /// </summary>
        public void Run(TimeSpan time)
        {
            for (var left = time; left > TimeSpan.Zero; left -= SimulationClock.Period)
            {
                Time.Advance(SimulationClock.Period);
                Clock.Tick();
            }
        }

        public bool Rides(Player player) => District.Ecs.Has<TransportRideComponent>(player.Entity);

        public void Dispose() => District.Dispose();
    }

    private static PlayerFrame[] Of(Player player, MoveOpcode command) =>
        player.Received().Where(e => e.Is(GroupOpcode.Move, (byte)command)).ToArray();

    private static PlayerFrame[] Refusals(Player player) =>
        player.Received().Where(e => e.Is(GroupOpcode.Move, (byte)((byte)MoveOpcode.TransportTake | 0x80))).ToArray();

    /// <summary>
    /// A player pays the fare and is told it rides, like the player beside it; the clock carries it for the seconds of the
    /// ride and a second more, and then it stands in the middle of the box the ride ends at and both are told it is over.
    /// What the rider's own client says about its place meanwhile moves nobody.
    /// </summary>
    [TestMethod]
    public async Task ARideIsPaidForToldToTheOthersAndEndedByTheClockAtTheBoxItEndsAt()
    {
        using var ferry = new Ferry();

        var rider = ferry.Join(gold: 5_000);
        var watcher = ferry.Join(gold: 0, x: 50f);
        var start = ferry.District.Ecs.Get<WorldComponent>(rider.Entity).Position;

        await ferry.Ask(rider, Ride);
        await ferry.District.SettleAsync(rider, watcher);

        Assert.AreEqual(4_000, ferry.Inventory.Gold(rider.Entity), "The fare was taken.");
        Assert.IsTrue(ferry.Rides(rider));

        foreach (var player in new[] { rider, watcher })
        {
            var told = Of(player, MoveOpcode.TransportTake).Single();

            Assert.AreEqual((rider.Actor, Ride, NpcActor), (BitConverter.ToInt32(told.Body, 0), BitConverter.ToInt16(told.Body, 4), BitConverter.ToInt32(told.Body, 6)));
            Assert.AreEqual(10, told.Body.Length, "The client reads actor, row and NPC.");
        }

        Assert.AreEqual(TimeSpan.FromSeconds(Seconds + 1), ferry.Map.Protected.Single().Length, "Nothing hurts the rider for the whole ride.");

        // The client of the rider reports places along the path: they are not moves.
        await ferry.District.MoveAsync(rider, 900f, 900f);
        await ferry.District.SettleAsync(rider, watcher);

        Assert.AreEqual(start, ferry.District.Ecs.Get<WorldComponent>(rider.Entity).Position);
        Assert.IsEmpty(rider.Moves().Concat(watcher.Moves()), "A move of a rider is not shown, not even to the rider.");

        // The seconds of the ride and the second it waits to set off, less a tick: still on the road.
        ferry.Run(TimeSpan.FromSeconds(Seconds));

        Assert.IsTrue(ferry.Rides(rider));
        Assert.IsEmpty(Of(rider, MoveOpcode.TransportOff));

        ferry.Run(TransportService.Departure + SimulationClock.Period);
        await ferry.District.SettleAsync(rider, watcher);

        Assert.IsFalse(ferry.Rides(rider));

        var arrived = ferry.District.Ecs.Get<WorldComponent>(rider.Entity);

        Assert.AreEqual((Station, 90f), (arrived.Position, arrived.Rotation));

        foreach (var player in new[] { rider, watcher })
        {
            Assert.AreEqual(rider.Actor, BitConverter.ToInt32(Of(player, MoveOpcode.TransportOff).Single().Body, 0));
        }

        // The rider moves by its own requests again.
        await ferry.District.MoveAsync(rider, Station.X + 50f, Station.Y);
        await ferry.District.SettleAsync(rider, watcher);

        Assert.IsNotEmpty(rider.Moves(), "A move after the ride is shown.");
    }

    /// <summary>
    /// Each reason a ride is refused answers with its own code, takes no gold and tells nobody: an NPC that is not there or
    /// stands too far, a ride the NPC does not offer, one that ends on no box of this map, too little gold, and a second
    /// ride while on the first.
    /// </summary>
    [TestMethod]
    public async Task ARideThatCannotBeTakenIsRefusedWithItsCodeAndCostsNothing()
    {
        using var ferry = new Ferry(boxOnTheMap: 1);

        var near = ferry.Join(gold: 5_000);
        var watcher = ferry.Join(gold: 0);

        async Task<ushort> RefusedAsync(Player player, short row, int npc = NpcActor)
        {
            player.Forget();

            await ferry.Ask(player, row, npc);
            await ferry.District.SettleAsync(player, watcher);

            return BitConverter.ToUInt16(Refusals(player).Single().Body, 0);
        }

        Assert.AreEqual(ServiceNpcCodes.Transport, await RefusedAsync(near, Ride, npc: 99), "No such NPC.");
        Assert.AreEqual(ServiceNpcCodes.TransportRide, await RefusedAsync(near, OtherRide), "The NPC does not offer that ride.");
        Assert.AreEqual(ServiceNpcCodes.TransportRide, await RefusedAsync(near, 0), "Row 0 is an empty place of the table.");
        Assert.AreEqual(ServiceNpcCodes.TransportRide, await RefusedAsync(near, Ride), "The map has no box to end at.");

        var far = ferry.Join(gold: 5_000, x: 5_000f);

        Assert.AreEqual(ServiceNpcCodes.Transport, await RefusedAsync(far, Ride), "Too far from the NPC.");

        Assert.AreEqual(5_000, ferry.Inventory.Gold(near.Entity));
        Assert.AreEqual(5_000, ferry.Inventory.Gold(far.Entity));
        Assert.IsFalse(ferry.Rides(near) || ferry.Rides(far));
        Assert.IsEmpty(Of(watcher, MoveOpcode.TransportTake), "Nobody is told of a ride that did not happen.");

        // The same map with its box: too little gold, then the ride, then a second one on the ride.
        using var withBox = new Ferry();

        var rider = withBox.Join(gold: Fare - 1);
        var second = withBox.Join(gold: 0);

        async Task<ushort> RefusedHereAsync(Player player)
        {
            player.Forget();

            await withBox.Ask(player, Ride);
            await withBox.District.SettleAsync(player, second);

            return BitConverter.ToUInt16(Refusals(player).Single().Body, 0);
        }

        Assert.AreEqual(ServiceNpcCodes.TransportGold, await RefusedHereAsync(rider), "One gold short.");
        Assert.AreEqual(Fare - 1, withBox.Inventory.Gold(rider.Entity));

        withBox.Inventory.TryAddGold(rider.Entity, Fare + 1);
        await withBox.Ask(rider, Ride);

        Assert.IsTrue(withBox.Rides(rider));
        Assert.AreEqual(Fare, withBox.Inventory.Gold(rider.Entity));
        Assert.AreEqual(ServiceNpcCodes.Transport, await RefusedHereAsync(rider), "A rider cannot take a second ride.");
        Assert.AreEqual(Fare, withBox.Inventory.Gold(rider.Entity), "The second ride took nothing.");
    }
}
