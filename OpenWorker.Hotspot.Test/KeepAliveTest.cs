using System.Net;
using System.Net.Sockets;
using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Channel;
using OpenWorker.DistrictServer.Server.Components;
using OpenWorker.DistrictServer.Server.Services;
using OpenWorker.DistrictServer.Services;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.System.Requests;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Hotspot.SoulWorker.Network;

namespace OpenWorker.Hotspot.Test;

[TestClass]
[DoNotParallelize]
public sealed class KeepAliveTest
{
    private const long ClientTickCount = 0x0102030405060708;

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        private int _errors;

        public int Errors => Volatile.Read(ref _errors);

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Error)
            {
                Interlocked.Increment(ref _errors);
            }
        }
    }

    private sealed class Fixture : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly TcpClient _server;

        public Fixture()
        {
            Time = new FakeTimeProvider();
            World = World.Create();
            Loop = new WorldLoop(NullLogger<WorldLoop>.Instance);

            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();

            Client = new TcpClient();
            Client.Connect(IPAddress.Loopback, ((IPEndPoint)_listener.LocalEndpoint).Port);
            Client.ReceiveTimeout = 5000;

            _server = _listener.AcceptTcpClient();

            Channels = new ServiceChannels(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Instance:Identifier"] = Guid.NewGuid().ToString(),
                    ["Channel:From"] = "1",
                    ["Channel:To"] = "1"
                })
                .Build(), World);

            Player = World.Create(new ServerSessionComponent(_server), KeepAliveComponent.Start(Time.GetTimestamp()));

            Logger = new RecordingLogger<KeepAliveService>();
            Service = new KeepAliveService(World, Loop, Channels, Time, Logger);
            Handler = new SystemService(World, Time);
        }

        public FakeTimeProvider Time { get; }

        public World World { get; }

        public WorldLoop Loop { get; }

        public TcpClient Client { get; }

        public ServiceChannels Channels { get; }

        public Entity Player { get; }

        public RecordingLogger<KeepAliveService> Logger { get; }

        public KeepAliveService Service { get; }

        public SystemService Handler { get; }

        public KeepAliveComponent State => World.Get<KeepAliveComponent>(Player);

        /// <summary>
        /// One period passes and the check runs, the way the timer of the service does it.
        /// </summary>
        public void CheckAfterOnePeriod()
        {
            Time.Advance(KeepAliveService.Frequency);

            Service.CheckPlayer(Player);
        }

        /// <summary>
        /// The client sends its keep-alive and the handler of the district takes it.
        /// </summary>
        public async Task AnswerAsync(long tickCount = ClientTickCount)
        {
            var stream = new MemoryStream(BitConverter.GetBytes(tickCount));

            await Handler.OnHandleAsync(
                new ServiceHandleContext(Player, CancellationToken.None),
                new SystemKeepAliveRequest(new BinaryReader(stream)));
        }

        /// <summary>
        /// The next frame the client receives, as opcode and body.
        /// </summary>
        public (byte Group, byte Command, byte[] Body) ReadFrame()
        {
            var reader = new BinaryReader(Client.GetStream());

            var header = new MessageHeader(reader);
            var payload = reader.ReadBytes(header.ContentLength);

            MessageEncryption.Exchange(payload.AsSpan(), header.Version);

            return (payload[0], payload[1], payload[2..]);
        }

        public bool IsClosedByServer()
        {
            // A closed connection reads as zero bytes at once; an open and quiet one only times out.
            Client.ReceiveTimeout = 300;

            try
            {
                return Client.GetStream().Read(new byte[1]) == 0;
            }

            catch (IOException)
            {
                return false;
            }
        }

        public void Dispose()
        {
            Client.Dispose();
            _server.Dispose();
            _listener.Dispose();
            World.Dispose();
        }
    }

    /// <summary>
    /// Lets time pass until the condition holds. The service makes its timer a moment after it starts,
    /// and time that passed before that fires nothing, so one jump is not enough.
    /// </summary>
    private static async Task AdvanceUntilAsync(FakeTimeProvider time, Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);

        while (!condition())
        {
            Assert.IsLessThan(deadline, DateTime.UtcNow, "The condition was not met in time.");

            time.Advance(KeepAliveService.Frequency);

            await Task.Delay(20);
        }
    }

    [TestMethod]
    public void AClientThatNeverAnswersIsDisconnectedAfterFiveMissedKeepAlives()
    {
        using var fixture = new Fixture();

        for (var missed = 1; missed <= KeepAliveService.MaxMissedAnswers; missed++)
        {
            fixture.CheckAfterOnePeriod();

            var (group, command, _) = fixture.ReadFrame();

            Assert.AreEqual((byte)GroupOpcode.System, group);
            Assert.AreEqual((byte)SystemOpcode.KeepAlive, command);
            Assert.AreEqual(missed, fixture.State.PenaltyCount);
        }

        Assert.IsFalse(fixture.IsClosedByServer(), "Five missed keep-alives are still forgiven.");

        fixture.CheckAfterOnePeriod();

        Assert.IsTrue(fixture.IsClosedByServer(), "The sixth check must disconnect the client.");
    }

    [TestMethod]
    public async Task AClientThatKeepsAnsweringIsNeverDisconnected()
    {
        using var fixture = new Fixture();

        for (var period = 0; period < 30; period++)
        {
            // The answer comes some time before the check, as a client that sends every so often would.
            fixture.Time.Advance(KeepAliveService.Frequency / 2);

            await fixture.AnswerAsync();

            fixture.Time.Advance(KeepAliveService.Frequency / 2);

            fixture.Service.CheckPlayer(fixture.Player);
        }

        Assert.AreEqual(0, fixture.State.PenaltyCount);
        Assert.IsFalse(fixture.IsClosedByServer());
    }

    [TestMethod]
    public async Task AnAnswerForgivesTheMissedKeepAlives()
    {
        using var fixture = new Fixture();

        for (var round = 0; round < 3; round++)
        {
            for (var missed = 1; missed < KeepAliveService.MaxMissedAnswers; missed++)
            {
                fixture.CheckAfterOnePeriod();
                fixture.ReadFrame();
            }

            Assert.AreEqual(KeepAliveService.MaxMissedAnswers - 1, fixture.State.PenaltyCount);

            await fixture.AnswerAsync();

            Assert.AreEqual(0, fixture.State.PenaltyCount);
        }

        Assert.IsFalse(fixture.IsClosedByServer());
    }

    [TestMethod]
    public async Task TheAnswerIsMeasuredOnTheClockOfTheServer()
    {
        using var fixture = new Fixture();

        fixture.Time.Advance(TimeSpan.FromMinutes(5));

        // Whatever the client puts in its tick count says nothing about the time of the server.
        await fixture.AnswerAsync(long.MaxValue);

        Assert.AreEqual(fixture.Time.GetTimestamp(), fixture.State.AnsweredAt);
        Assert.AreEqual(long.MaxValue, fixture.State.ClientTickCount);

        fixture.Service.CheckPlayer(fixture.Player);

        Assert.AreEqual(0, fixture.State.PenaltyCount);
    }

    [TestMethod]
    public async Task TheKeepAliveGivesBackTheTickCountOfTheClient()
    {
        using var fixture = new Fixture();

        await fixture.AnswerAsync(ClientTickCount);

        fixture.CheckAfterOnePeriod();

        var (_, _, body) = fixture.ReadFrame();

        // The same eight bytes the client sent: this is the wire format of the message and it does not change.
        CollectionAssert.AreEqual(BitConverter.GetBytes(ClientTickCount), body);
    }

    [TestMethod]
    public void TheKeepAliveRequestIsOneInt64()
    {
        var bytes = BitConverter.GetBytes(ClientTickCount);

        var request = new SystemKeepAliveRequest(new BinaryReader(new MemoryStream(bytes)));

        Assert.AreEqual(ClientTickCount, request.TickCount);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        new SystemKeepAliveResponse { Time = new TimeSpan(ClientTickCount) }.Write(writer);
        writer.Flush();

        CollectionAssert.AreEqual(bytes, stream.ToArray());
    }

    [TestMethod]
    public async Task AFailedCheckIsLoggedAndTheNextOneStillRuns()
    {
        using var fixture = new Fixture();

        // A channel that cannot be walked: every check of the players fails on it.
        fixture.Channels.Add(default);

        await fixture.Service.StartAsync(CancellationToken.None);

        await AdvanceUntilAsync(fixture.Time, () => fixture.Logger.Errors >= 1);

        Assert.IsFalse(fixture.Service.ExecuteTask!.IsCompleted, "One failed check must not stop the service.");

        var failures = fixture.Logger.Errors;

        await AdvanceUntilAsync(fixture.Time, () => fixture.Logger.Errors > failures);

        await fixture.Service.StopAsync(CancellationToken.None);

        Assert.IsTrue(fixture.Service.ExecuteTask.IsCompletedSuccessfully, "Stopping must end the loop without an error.");
    }

    [TestMethod]
    public async Task StoppingTheServiceEndsTheLoopWithoutAnError()
    {
        using var fixture = new Fixture();

        await fixture.Service.StartAsync(CancellationToken.None);

        // Nobody is on the channel, so the checks have nothing to send and nothing to fail on.
        for (var period = 0; period < 3; period++)
        {
            fixture.Time.Advance(KeepAliveService.Frequency);

            await fixture.Loop.InvokeAsync(() => { });
        }

        await fixture.Service.StopAsync(CancellationToken.None);

        Assert.IsTrue(fixture.Service.ExecuteTask!.IsCompletedSuccessfully);
        Assert.AreEqual(0, fixture.Logger.Errors);
    }
}
