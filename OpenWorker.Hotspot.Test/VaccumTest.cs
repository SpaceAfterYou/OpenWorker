using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Modules.Drops;
using OpenWorker.Gameplay.Modules.Rewards;
using OpenWorker.Gameplay.Modules.Vaccum;
using OpenWorker.Hotspot.SoulWorker.Network;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// Creating entities registers component types in a registry that does not take two threads at once.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class VaccumTest
{
    private sealed class Rewards(VaccumPick pick, bool fits = true, int picks = 1) : IVaccumRewards, IRewardGranter
    {
        public List<Reward> Granted { get; } = [];

        public VaccumPick Roll(int interaction) => pick;

        public VaccumCubeRule? RuleOf(int interaction) => new VaccumCubeRule(TimeSpan.FromMilliseconds(50), picks, picks);

        public ValueTask<bool> TryGrantAsync(Entity player, Reward granted, RewardSource source, CancellationToken cancellationToken)
        {
            Granted.Add(granted);

            return ValueTask.FromResult(fits);
        }
    }

    private sealed class Harness : IDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly TcpClient _server;

        public Harness(Rewards rewards)
        {
            _listener.Start();

            Client = new TcpClient();
            Client.Connect(IPAddress.Loopback, ((IPEndPoint)_listener.LocalEndpoint).Port);
            Client.ReceiveTimeout = 2000;

            _server = _listener.AcceptTcpClient();

            Loop = new WorldLoop(NullLogger<WorldLoop>.Instance);
            World = Arch.Core.World.Create();
            Manager = new VaccumManager(World, rewards, rewards, NullLogger<VaccumManager>.Instance);

            Player = World.Create(
                Component.GetComponentType(typeof(ServerSessionComponent)),
                Component.GetComponentType(typeof(ActorComponent)),
                Component.GetComponentType(typeof(VaccumComponent)));

            World.Set(Player, new ServerSessionComponent(_server));
            World.Set(Player, new ActorComponent(42, ActorType.User));
            World.Set(Player, default(VaccumComponent));

            Reader = new BinaryReader(Client.GetStream());
        }

        public TcpClient Client { get; }
        public WorldLoop Loop { get; }
        public World World { get; }
        public VaccumManager Manager { get; }
        public Entity Player { get; }
        public BinaryReader Reader { get; }

        /// <summary>
        /// The next frame the client gets: group, command and the rest.
        /// </summary>
        public (byte Group, byte Command, byte[] Body) Next()
        {
            var header = new MessageHeader(Reader);
            var content = Reader.ReadBytes(header.ContentLength);

            MessageEncryption.Exchange(content, header.Version);

            return (content[0], content[1], content[2..]);
        }

        /// <summary>
        /// Whether nothing arrives for a while.
        /// </summary>
        public bool Quiet(int milliseconds)
        {
            Client.ReceiveTimeout = milliseconds;

            try
            {
                Next();

                return false;
            }

            catch (IOException)
            {
                return true;
            }

            finally
            {
                Client.ReceiveTimeout = 2000;
            }
        }

        public void Dispose()
        {
            Reader.Dispose();
            Client.Dispose();
            _server.Dispose();
            _listener.Stop();
        }
    }

    private static VaccumPick OneItem => new(710000001, 2, 0);

    [TestMethod]
    public async Task RegisteringACubeTellsTheClient()
    {
        using var test = new Harness(new Rewards(OneItem));

        await test.Loop.InvokeAsync(() => test.Manager.Register(test.Player, 30001, 7, TimeSpan.FromMilliseconds(50)));

        var (group, command, body) = test.Next();

        Assert.AreEqual(0x25, group);
        Assert.AreEqual(0x11, command);
        CollectionAssert.AreEqual(BitConverter.GetBytes(30001).Concat(BitConverter.GetBytes(7)).Append((byte)0).ToArray(), body);
    }

    /// <summary>
    /// A hold that is kept to the end gives a pick and lets the cube go; the cube stays for the picks it has left and goes with
    /// the last, and the client is told how many remain.
    /// </summary>
    [TestMethod]
    public async Task ACubeGivesOnePickPerHoldAndGoesWithItsLast()
    {
        var rewards = new Rewards(OneItem, picks: 2);

        using var test = new Harness(rewards);

        await test.Loop.InvokeAsync(() => test.Manager.Register(test.Player, 30001, 7, TimeSpan.FromMilliseconds(80)));
        test.Next();

        await test.Loop.InvokeAsync(() => test.Manager.ClickStart(test.Player, 30001));

        var start = test.Next();

        Assert.AreEqual(0x01, start.Command);
        CollectionAssert.AreEqual(BitConverter.GetBytes(42).Concat(BitConverter.GetBytes(30001)).Concat(BitConverter.GetBytes(0)).ToArray(), start.Body);

        var complete = test.Next();

        Assert.AreEqual(0x03, complete.Command);
        Assert.AreEqual(42, BitConverter.ToInt32(complete.Body));
        Assert.AreEqual(30001, BitConverter.ToInt32(complete.Body, 4));
        Assert.AreEqual((short)1, BitConverter.ToInt16(complete.Body, 8), "One pick remains");
        Assert.AreEqual((byte)1, complete.Body[10]);
        Assert.AreEqual(710000001, BitConverter.ToInt32(complete.Body, 11));
        Assert.AreEqual((short)2, BitConverter.ToInt16(complete.Body, 15));
        Assert.AreEqual(0, BitConverter.ToInt32(complete.Body, 17));

        var released = test.Next();

        Assert.AreEqual(0x02, released.Command, "The cube is let go after a pick");
        Assert.AreEqual(VaccumManager.Released, BitConverter.ToInt32(released.Body, 8));

        // The second hold takes the last pick, and the cube is gone.
        await test.Loop.InvokeAsync(() => test.Manager.ClickStart(test.Player, 30001));

        Assert.AreEqual(0x01, test.Next().Command);

        var last = test.Next();

        Assert.AreEqual(0x03, last.Command);
        Assert.AreEqual((short)0, BitConverter.ToInt16(last.Body, 8));
        Assert.AreEqual(0x02, test.Next().Command);

        var gone = test.Next();

        Assert.AreEqual(0x12, gone.Command);
        Assert.AreEqual(30001, BitConverter.ToInt32(gone.Body));
        Assert.HasCount(2, rewards.Granted);
    }

    [TestMethod]
    public async Task LettingGoBeforeTheTimeFreesTheCube()
    {
        using var test = new Harness(new Rewards(OneItem));

        await test.Loop.InvokeAsync(() => test.Manager.Register(test.Player, 30001, 7, TimeSpan.FromMilliseconds(300)));
        test.Next();

        await test.Loop.InvokeAsync(() => test.Manager.ClickStart(test.Player, 30001));
        test.Next();

        await test.Loop.InvokeAsync(() => test.Manager.ClickCancel(test.Player, 30001));

        var cancel = test.Next();

        Assert.AreEqual(0x02, cancel.Command);
        Assert.AreEqual(VaccumManager.Released, BitConverter.ToInt32(cancel.Body, 8));
        Assert.IsTrue(test.Quiet(500), "nothing is given for a cube that was let go");

        // The cube is still there to be taken again.
        await test.Loop.InvokeAsync(() => test.Manager.ClickStart(test.Player, 30001));

        Assert.AreEqual(0x01, test.Next().Command);
    }

    /// <summary>
    /// A person takes one cube at a time: any click while another is held, or the same, is refused as busy.
    /// </summary>
    [TestMethod]
    public async Task OnlyOneCubeIsTakenAtATime()
    {
        using var test = new Harness(new Rewards(default));

        await test.Loop.InvokeAsync(() =>
        {
            test.Manager.Register(test.Player, 1, 0, TimeSpan.FromMilliseconds(400));
            test.Manager.Register(test.Player, 2, 0, TimeSpan.FromMilliseconds(400));
        });

        test.Next();
        test.Next();

        await test.Loop.InvokeAsync(() => test.Manager.ClickStart(test.Player, 1));
        test.Next();

        await test.Loop.InvokeAsync(() => test.Manager.ClickStart(test.Player, 2));

        var busy = test.Next();

        Assert.AreEqual(0x01, busy.Command);
        Assert.AreEqual(VaccumManager.Busy, BitConverter.ToInt32(busy.Body, 8));

        await test.Loop.InvokeAsync(() => test.Manager.ClickStart(test.Player, 1));

        Assert.AreEqual(VaccumManager.Busy, BitConverter.ToInt32(test.Next().Body, 8), "The same cube too");
    }

    /// <summary>
    /// Every click and every release gets an answer: a cube that is not there, or a release of a cube that is not held, is told
    /// with the code of the game.
    /// </summary>
    [TestMethod]
    public async Task ACubeThatIsNotThereOrNotHeldIsAnsweredWithItsCode()
    {
        using var test = new Harness(new Rewards(OneItem));

        await test.Loop.InvokeAsync(() =>
        {
            test.Manager.ClickStart(test.Player, 99);
            test.Manager.ClickCancel(test.Player, 99);
            test.Manager.Register(test.Player, 5, 0, TimeSpan.FromMilliseconds(300));
        });

        var start = test.Next();
        var cancel = test.Next();

        Assert.AreEqual(0x01, start.Command);
        Assert.AreEqual(VaccumManager.Released, BitConverter.ToInt32(start.Body, 8));
        Assert.AreEqual(0x02, cancel.Command);
        Assert.AreEqual(VaccumManager.Released, BitConverter.ToInt32(cancel.Body, 8));

        test.Next();

        await test.Loop.InvokeAsync(() => test.Manager.ClickCancel(test.Player, 5));

        Assert.AreEqual(VaccumManager.NotHeld, BitConverter.ToInt32(test.Next().Body, 8));
    }

    /// <summary>
    /// A pick whose reward does not fit is refused with the code of the thing that was full, gives nothing and uses up nothing: the
    /// cube stays to be taken again.
    /// </summary>
    [TestMethod]
    public async Task APickThatDoesNotFitIsRefusedWithTheCodeOfItsMoneyAndTheCubeStays()
    {
        using var test = new Harness(new Rewards(new VaccumPick(DropService.GoldObject, 1100, 0), false));

        await test.Loop.InvokeAsync(() => test.Manager.Register(test.Player, 5, 0, TimeSpan.FromMilliseconds(50)));
        test.Next();
        await test.Loop.InvokeAsync(() => test.Manager.ClickStart(test.Player, 5));
        test.Next();

        var complete = test.Next();

        Assert.AreEqual(0x03, complete.Command);
        Assert.AreEqual((short)1, BitConverter.ToInt16(complete.Body, 8), "The pick is not used up");
        Assert.AreEqual((byte)0, complete.Body[10]);
        Assert.AreEqual(VaccumManager.NoRoomForGold, BitConverter.ToInt32(complete.Body, 11));
        Assert.AreEqual(0x02, test.Next().Command, "The cube is let go");

        await test.Loop.InvokeAsync(() => test.Manager.ClickStart(test.Player, 5));

        Assert.AreEqual(0x01, test.Next().Command);
    }

    private static Action<BinaryWriter> CubeObject(uint id, uint reward) => writer =>
    {
        writer.Write(id);
        writer.Write(CubeObjectRow.CubeKind);
        writer.Write((byte)0);
        writer.Write(12000u);
        writer.Write((byte)0);
        writer.Write((ushort)0);
        writer.Write(new byte[3]);
        writer.Write(new byte[7 * 4]);
        writer.Write((short)1);
        writer.Write((short)3);
        writer.Write(new byte[8]);
        writer.Write((byte)1);
        writer.Write(new byte[16]);
        writer.Write((ushort)1);
        writer.Write(300000u);
        writer.Write(reward);
        writer.Write(new byte[5]);
        writer.Write((ushort)0);
    };

    private static Action<BinaryWriter> CubeReward(uint id, ushort[] chances, uint[] items, ushort[] least, ushort[] most) => writer =>
    {
        writer.Write(id);

        foreach (var value in chances)
        {
            writer.Write(value);
        }

        foreach (var value in items)
        {
            writer.Write(value);
        }

        foreach (var value in least)
        {
            writer.Write(value);
        }

        foreach (var value in most)
        {
            writer.Write(value);
        }
    };

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

    /// <summary>
    /// A source of numbers that gives the ones it was given, in order.
    /// </summary>
    private sealed class Numbers(params int[] values) : Random
    {
        private int _next;

        public override int Next(int minValue, int maxValue) => values[_next++ % values.Length];
    }

    /// <summary>
    /// The pick of a cube is the place of its row that brings a roll from one to ten thousand to zero as the chances are taken off
    /// in turn, with a number between its least and its most; a roll past the last place, a row that gives only up to a hundred, a
    /// place that has a chance and no item and an object the tables lack give what the original gives.
    /// </summary>
    [TestMethod]
    public void ThePickIsThePlaceTheRollFallsInto()
    {
        ushort[] none = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

        var rewards = new InteractionRewards(
            Rows<CubeObjectRow>(CubeObject(50050, 50), CubeObject(50010, 10), CubeObject(50020, 20)),
            Rows<CubeRewardRow>(
                CubeReward(50, [3500, 3500, 200, 0, 0, 0, 0, 0, 0, 0], [800000001, 800000004, 720101001, 0, 0, 0, 0, 0, 0, 0], [1080, 38, 1, 0, 0, 0, 0, 0, 0, 0], [1315, 54, 2, 0, 0, 0, 0, 0, 0, 0]),
                CubeReward(10, [80, 10, 4, 0, 0, 0, 0, 0, 0, 0], [800000001, 720100001, 710100101, 0, 0, 0, 0, 0, 0, 0], none, none),
                CubeReward(20, [100, 0, 0, 0, 0, 0, 0, 0, 0, 0], [0, 0, 0, 0, 0, 0, 0, 0, 0, 0], none, none)));

        // The roll, then the number inside the place.
        Assert.AreEqual(new VaccumPick(800000001, 1200, 0), rewards.Roll(50050, new Numbers(3500, 1200)), "The first place keeps the first 3500");
        Assert.AreEqual(new VaccumPick(800000004, 40, 0), rewards.Roll(50050, new Numbers(3501, 40)), "Then the second");
        Assert.AreEqual(new VaccumPick(720101001, 2, 0), rewards.Roll(50050, new Numbers(7200, 2)));
        Assert.AreEqual(default, rewards.Roll(50050, new Numbers(7201, 0)), "Past the last place with a chance: nothing");
        Assert.AreEqual(default, rewards.Roll(50010, new Numbers(5000, 0)), "A row whose chances add to a hundred gives nothing to a roll past it");
        Assert.AreEqual(new VaccumPick(0, 0, InteractionRewards.NoItemInPlace), rewards.Roll(50020, new Numbers(100, 0)), "A chance with no item");
        Assert.AreEqual(default, rewards.Roll(1, new Numbers(1, 1)), "An object the tables lack");
        Assert.AreEqual(new VaccumCubeRule(TimeSpan.FromSeconds(12), 1, 3), rewards.RuleOf(50050));
        Assert.IsNull(rewards.RuleOf(1));
    }
}
