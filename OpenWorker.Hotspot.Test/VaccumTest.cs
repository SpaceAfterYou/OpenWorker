using System.Net;
using System.Net.Sockets;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Modules.Rewards;
using OpenWorker.Gameplay.Modules.Vaccum;
using OpenWorker.Hotspot.SoulWorker.Network;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// Creating entities registers component types in a registry that does not take two threads at once.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class VaccumTest
{
    private sealed class Rewards(Reward reward, bool fits = true) : IVaccumRewards, IRewardGranter
    {
        public Reward Roll(int value) => reward;

        public ValueTask<bool> TryGrantAsync(Entity player, Reward granted, RewardSource source, CancellationToken cancellationToken) =>
            ValueTask.FromResult(fits);
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

    private static Reward OneItem => new([new RewardItem(710000001, 2)]);

    [TestMethod]
    public async Task RegisteringACubeTellsTheClient()
    {
        using var test = new Harness(new Rewards(Reward.Empty));

        await test.Loop.InvokeAsync(() => test.Manager.Register(test.Player, 30001, 7, TimeSpan.FromMilliseconds(50)));

        var (group, command, body) = test.Next();

        Assert.AreEqual(0x25, group);
        Assert.AreEqual(0x11, command);
        CollectionAssert.AreEqual(BitConverter.GetBytes(30001).Concat(BitConverter.GetBytes(7)).Append((byte)0).ToArray(), body);
    }

    [TestMethod]
    public async Task HoldingACubeGivesTheRewardAndRemovesIt()
    {
        using var test = new Harness(new Rewards(OneItem));

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
        Assert.AreEqual((byte)1, complete.Body[10]);
        Assert.AreEqual(710000001, BitConverter.ToInt32(complete.Body, 11));
        Assert.AreEqual((short)2, BitConverter.ToInt16(complete.Body, 15));
        Assert.AreEqual(0, BitConverter.ToInt32(complete.Body, 17));

        var gone = test.Next();

        Assert.AreEqual(0x12, gone.Command);
        Assert.AreEqual(30001, BitConverter.ToInt32(gone.Body));

        await test.Loop.InvokeAsync(() => test.Manager.ClickStart(test.Player, 30001));

        Assert.IsTrue(test.Quiet(200), "a cube that was taken is gone");
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

    [TestMethod]
    public async Task OnlyOneCubeIsTakenAtATime()
    {
        using var test = new Harness(new Rewards(Reward.Empty));

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
    }

    [TestMethod]
    public async Task ACubeThatIsNotThereIsIgnored()
    {
        using var test = new Harness(new Rewards(OneItem));

        await test.Loop.InvokeAsync(() =>
        {
            test.Manager.ClickStart(test.Player, 99);
            test.Manager.ClickCancel(test.Player, 99);
        });

        Assert.IsTrue(test.Quiet(200));
    }

    [TestMethod]
    public async Task RewardThatDoesNotFitIsReportedAndTheCubeStaysGone()
    {
        using var test = new Harness(new Rewards(OneItem, false));

        await test.Loop.InvokeAsync(() => test.Manager.Register(test.Player, 5, 0, TimeSpan.FromMilliseconds(50)));
        test.Next();
        await test.Loop.InvokeAsync(() => test.Manager.ClickStart(test.Player, 5));
        test.Next();

        var complete = test.Next();

        Assert.AreEqual(0x03, complete.Command);
        Assert.AreEqual((byte)0, complete.Body[10]);
        Assert.AreEqual(VaccumManager.CannotTake, BitConverter.ToInt32(complete.Body, 11));
    }
}
