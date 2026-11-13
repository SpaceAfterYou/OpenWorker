using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Attendance;
using OpenWorker.Gameplay.Modules.Attendance.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Rewards;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.DistrictServer;
using OpenWorker.Hotspot.Handler.Abstractions;
using OpenWorker.Hotspot.Modules.Events.Requests;
using OpenWorker.MazeServer;
using OpenWorker.Server.Common.Services;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The rewards of play time: counted by the server, given when the person claims them.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class PlayTimeClaimTest
{
    private const int Item = 837100001;

    [TestMethod]
    public async Task TwoClaimsAtOnceGiveTheStepOnce()
    {
        using var f = new Fixture();

        f.Time.Advance(TimeSpan.FromMinutes(1));
        f.Manager.CountPlayTime(f.Player);
        Assert.AreEqual(0, f.Granter.Given.Count, "a reached step waits for the claim");

        f.Granter.Gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var first = f.Manager.ClaimPlayTimeAsync(f.Player, CancellationToken.None).AsTask();
        var second = f.Manager.ClaimPlayTimeAsync(f.Player, CancellationToken.None).AsTask();

        f.Granter.Gate.SetResult(true);
        await Task.WhenAll(first, second);

        Assert.HasCount(1, f.Granter.Given);
        Assert.AreEqual(1, f.Book.PlayStep);

        await MetricAssertions.UntilAsync(() => f.Received(EventOpcode.AttendancePlayTimeReward) == 1, "the claim that was given is told");
        await MetricAssertions.UntilAsync(() => f.Received(EventOpcode.AttendanceLoad) == 1, "the other one sets the countdown again");
    }

    [TestMethod]
    public async Task TheSecondsPlayedGoToTheFlushUpToTheClose()
    {
        using var f = new Fixture();

        var dirty = new PersonDirty(7);

        f.World.Add(f.Player, new DirtyComponent(dirty));

        f.Time.Advance(TimeSpan.FromSeconds(20));
        await f.Manager.OnSessionClosedAsync(f.Player);

        Assert.AreEqual(20, f.Book.PlaySeconds, "the seconds since the last count are counted at the close");
        Assert.AreEqual(PersonSections.MiscState, dirty.Sections & PersonSections.MiscState, "and written by the flush that follows");
    }

    [TestMethod]
    public void AMazeCountsAndAnswersPlayTimeAsTheDistrictDoes()
    {
        var shared = typeof(EventService).Assembly.GetName().Name;

        Assert.IsTrue(typeof(IHotspotHandler<EventAttendancePlayTimeRewardRequest>).IsAssignableFrom(typeof(EventService)));

        foreach (var server in new[] { typeof(MazeServerHost), typeof(DistrictServerHost) })
        {
            Assert.IsTrue(server.Assembly.GetReferencedAssemblies().Any(e => e.Name == shared), server.Name);
        }

        Assert.IsTrue(typeof(MiscStateLoader).GetConstructors().Single().GetParameters().Any(e => e.ParameterType == typeof(AttendanceManager)),
            "a maze loads the book of the person");
    }

    private sealed class Fixture : IDisposable
    {
        public World World { get; } = World.Create();
        public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 11, 7, 12, 0, 0, TimeSpan.Zero));
        public RecordingGranter Granter { get; } = new();
        public AttendanceManager Manager { get; }
        public AttendanceBook Book { get; }
        public Entity Player { get; }

        private readonly RecordingConnection _connection = new();

        public Fixture()
        {
            var catalog = AttendanceCatalog.From(
                [new AttendanceTable(1011, 28)],
                new Dictionary<int, Reward>(),
                [new PlayTimeStep(60, Item), new PlayTimeStep(120, Item), new PlayTimeStep(180, Item)]);

            var contents = new ServerContent { [ServerContentIndex.Attendance] = true };

            Manager = new AttendanceManager(
                World, new PersonStateStore(null!), catalog, Granter, contents, Time,
                Options.Create(new AttendanceOptions()), NullLogger<AttendanceManager>.Instance);

            Book = new AttendanceBook { Table = 1011, Count = 1, LastDay = Time.GetLocalNow(), PlayDay = Time.GetLocalNow() };

            Player = World.Create(new ServerSessionComponent(_connection));
            World.Add(Player, new AttendanceComponent { Person = 7, Book = Book, Sent = true, LastTick = Time.GetLocalNow() });
        }

        public int Received(EventOpcode command)
        {
            return _connection.Frames.Select(PlayerFrame.Open).Count(e => e.Is(GroupOpcode.Event, (byte)command));
        }

        public void Dispose()
        {
            World.Dispose();
        }
    }

    private sealed class RecordingGranter : IRewardGranter
    {
        public List<Reward> Given { get; } = [];

        public TaskCompletionSource<bool>? Gate { get; set; }

        public async ValueTask<bool> TryGrantAsync(Entity player, Reward reward, RewardSource source, CancellationToken cancellationToken)
        {
            Given.Add(reward);

            return Gate is { } gate ? await gate.Task : true;
        }
    }
}
