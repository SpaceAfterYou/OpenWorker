using System.Buffers;
using System.Collections.Concurrent;
using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenWorker.Channel;
using OpenWorker.DependencyInjection;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Channels.Components;
using OpenWorker.Gameplay.Modules.League.Components;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.SoulWorker.Network;
using OpenWorker.Hotspot.Telemetry;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A connection that keeps a copy of every frame written to it, and can hold the writes back until a test lets them go.
/// </summary>
internal sealed class RecordingConnection : Stream
{
    private readonly ConcurrentQueue<byte[]> _frames = new();

    /// <summary>
    /// What every write waits for; <see langword="null"/> to write at once.
    /// </summary>
    public TaskCompletionSource? Gate { get; init; }

    public byte[][] Frames => _frames.ToArray();

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (Gate is { } gate)
        {
            await gate.Task.WaitAsync(cancellationToken);
        }

        _frames.Enqueue(buffer.ToArray());
    }

    public override void Write(byte[] buffer, int offset, int count) => _frames.Enqueue(buffer.AsSpan(offset, count).ToArray());

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();
}

/// <summary>
/// An array pool that knows which of its arrays are out, and counts an array that comes back twice.
/// </summary>
internal sealed class WatchedArrayPool : ArrayPool<byte>
{
    private readonly Lock _lock = new();
    private readonly HashSet<byte[]> _out = [];

    public int Rented { get; private set; }

    public int Returned { get; private set; }

    public int ReturnedTwice { get; private set; }

    public int Outstanding
    {
        get
        {
            lock (_lock)
            {
                return _out.Count;
            }
        }
    }

    public override byte[] Rent(int minimumLength)
    {
        var array = new byte[minimumLength];

        lock (_lock)
        {
            _out.Add(array);
            Rented++;
        }

        return array;
    }

    public override void Return(byte[] array, bool clearArray = false)
    {
        lock (_lock)
        {
            if (_out.Remove(array))
            {
                Returned++;
            }

            else
            {
                ReturnedTwice++;
            }
        }
    }
}

/// <summary>
/// Players of a district who can join a channel, over connections that are only streams.
/// </summary>
internal static class ChannelMembers
{
    /// <summary>
    /// Gives a player every component of a district, as the server does.
    /// </summary>
    public static ArchComponentProvider Provider()
    {
        // The provider looks for the types of the components in the loaded assemblies.
        _ = typeof(ChannelMemberComponent);
        _ = typeof(ServiceChannels);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [ConfigurationUtils.InstanceTargetKey] = nameof(EntityComponentService.District) })
            .Build();

        return new ArchComponentProvider(configuration, NullLogger<ArchComponentProvider>.Instance);
    }

    /// <summary>
    /// One channel.
    /// </summary>
    public static ServiceChannels Channels(World world)
    {
        return new ServiceChannels(
            Options.Create(new ChannelOptions { From = 1, To = 1 }),
            Options.Create(new InstanceOptions { Identifier = Guid.NewGuid() }),
            world);
    }

    /// <summary>
    /// A player with what the others are told about it when it joins.
    /// </summary>
    public static Entity Create(World world, ArchComponentProvider provider, Stream connection, int index)
    {
        var player = provider.CreateEntity(world);

        world.Set(player, new ServerSessionComponent(connection));
        world.Set(player, new PersonInfoComponent { Name = "Player" + index, Hero = Hero.Lily });
        world.Set(player, new LeagueComponent { Id = 0, Name = string.Empty, Card = new LeagueComponentCard { Border = 0, Emblem = 0 } });
        world.Set(player, new ShopPrivateComponent { Type = 0, Name = string.Empty });

        return player;
    }
}

/// <summary>
/// Creating entities registers component types in a registry that does not take two threads at once.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class FrameTest
{
    private static readonly MessageOpcode FillerOpcode = new(GroupOpcode.Move, MoveOpcode.MoveBt);

    private static readonly MessageOpcode CountingOpcode = new(GroupOpcode.System, (SystemOpcode)0x7D);

    /// <summary>
    /// A body of <c>size + 1</c> bytes of one value after the opcode.
    /// </summary>
    private sealed class Filler(byte value, int size) : IResponseHotspotMessage
    {
        public MessageOpcode Opcode => FillerOpcode;

        public void Write(BinaryWriter writer)
        {
            var filler = new byte[size + 1];

            Array.Fill(filler, value);

            writer.Write(filler);
        }
    }

    /// <summary>
    /// Counts how many times it is written.
    /// </summary>
    private sealed class Counting : IResponseHotspotMessage
    {
        public int Writes { get; private set; }

        public MessageOpcode Opcode => CountingOpcode;

        public void Write(BinaryWriter writer)
        {
            Writes++;

            writer.Write(0x01020304);
            writer.Write("frame");
        }
    }

    private static MessageOpcode OpcodeOf(byte[] frame)
    {
        var header = new MessageHeader(frame);
        var body = frame.AsSpan(MessageHeader.HeaderSize, header.ContentLength).ToArray();

        MessageEncryption.Exchange(body.AsSpan(), header.Version);

        return new MessageOpcode(body[0], body[1]);
    }

    [TestMethod]
    public async Task ABroadcastIsWrittenOnceAndEveryMemberGetsTheSameBytes()
    {
        var provider = ChannelMembers.Provider();

        using var world = World.Create();

        var channels = ChannelMembers.Channels(world);

        var connections = Enumerable.Range(0, 5).Select(_ => new RecordingConnection()).ToArray();

        for (var index = 0; index < connections.Length; index++)
        {
            channels.Join(0, ChannelMembers.Create(world, provider, connections[index], index));
        }

        var message = new Counting();

        channels[0].Broadcast(message);

        Assert.AreEqual(1, message.Writes, "A broadcast encodes the message once, whatever the number of members.");

        // Each member gets the joins of the players after it first, then the broadcast.
        await MetricAssertions.UntilAsync(
            () => connections.All(e => e.Frames is { Length: > 0 } frames && (short)OpcodeOf(frames[^1]) == CountingOpcode),
            "Every member gets the broadcast.");

        var first = connections[0].Frames[^1];

        foreach (var connection in connections)
        {
            CollectionAssert.AreEqual(first, connection.Frames[^1]);
        }
    }

    [TestMethod]
    public async Task TheArrayOfAFrameGoesBackOnceAfterTheLastConnectionLetsItGo()
    {
        var arrays = new WatchedArrayPool();

        using var encoder = new FrameEncoder(NullLogger.Instance, arrays: arrays);

        var written = new RecordingConnection();
        var held = new RecordingConnection { Gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously) };
        var gone = new RecordingConnection();

        var writes = new ServerSessionComponent(written);
        var holds = new ServerSessionComponent(held);
        var drops = new ServerSessionComponent(gone);

        drops.Disconnect();

        await drops.Closed.WaitAsync(TimeSpan.FromSeconds(5));

        // Longer than the first array an encoder rents, so the arrays it grows through are watched too.
        using (var frame = encoder.Encode(new Filler(7, 1000)))
        {
            Assert.IsNotNull(frame);

            Assert.IsTrue(writes.TrySend(frame));
            Assert.IsTrue(holds.TrySend(frame));
            Assert.IsFalse(drops.TrySend(frame), "A closed connection lets the frame go at once.");
        }

        await MetricAssertions.UntilAsync(() => written.Frames.Length == 1, "The first connection writes the frame.");

        Assert.AreEqual(1, arrays.Outstanding, "The connection that has not written yet still holds the array.");

        held.Gate.SetResult();

        await MetricAssertions.UntilAsync(() => arrays.Outstanding == 0, "The array goes back after the last write.");

        Assert.AreEqual(arrays.Rented, arrays.Returned);
        Assert.AreEqual(0, arrays.ReturnedTwice, "An array goes back to the pool once.");
        CollectionAssert.AreEqual(written.Frames[0], held.Frames[0]);
    }

    [TestMethod]
    public async Task AMessageLongerThanAFrameIsDroppedAndTheSessionStaysOpen()
    {
        using var metrics = new MetricsHost();
        using var dropped = metrics.Listen<long>("frames.dropped");

        var logger = new CapturingLogger<WorldLoop>();

        using var loop = new WorldLoop(logger, sessions: metrics.Get<SessionMetrics>());

        var connection = new RecordingConnection();
        var session = new ServerSessionComponent(connection);

        // The opcode takes two bytes of the body: the first message is one byte too long, the second fills a frame exactly.
        var (tooLong, full, after) = await loop.InvokeAsync(() => (
            session.TrySend(new Filler(1, MessageHeader.MaxContentLength - 2)),
            session.TrySend(new Filler(2, MessageHeader.MaxContentLength - 3)),
            session.TrySend(new Filler(3, 10))));

        Assert.IsFalse(tooLong);
        Assert.IsTrue(full);
        Assert.IsTrue(after);

        await MetricAssertions.UntilAsync(() => connection.Frames.Length == 2, "The two messages that fit are sent.");

        Assert.AreEqual(ushort.MaxValue, connection.Frames[0].Length);
        Assert.AreEqual(MessageHeader.HeaderSize + 2 + 11, connection.Frames[1].Length);
        Assert.IsFalse(session.Closed.IsCompleted, "A message that does not fit does not close the session.");

        Assert.AreEqual(1, dropped.Sum("opcode", FillerOpcode.ToFullString()));

        var warning = logger.Entries.Single(e => e.Level == LogLevel.Warning);

        Assert.AreEqual(FillerOpcode.ToFullString(), warning.Values["Opcode"]);
    }
}
