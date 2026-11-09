using OpenWorker.Channel.Community;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Test;

[TestClass]
[DoNotParallelize]
public sealed class CommunityNetworkTest
{
    private readonly struct Ping(int value) : IResponseHotspotMessage
    {
        public MessageOpcode Opcode => new(GroupOpcode.Chat, (byte)ChatOpcode.Notify);

        public void Write(BinaryWriter writer)
        {
            writer.Write(value);
        }
    }

    private static int ValueOf(ReceivedFrame frame) => BitConverter.ToInt32(frame.Content);

    [TestMethod]
    public void APresenceRecordSurvivesItsEncoding()
    {
        var record = new PresenceRecord(Guid.NewGuid(), 77, "Ab cD", 3, 25, 10003, 4, true, 1, 2);

        Assert.AreEqual(record, PresenceRecord.Decode(record.Encode()));
    }

    [TestMethod]
    public void ABrokenPresenceRecordIsNobody()
    {
        Assert.IsNull(PresenceRecord.Decode(null));
        Assert.IsNull(PresenceRecord.Decode(string.Empty));
        Assert.IsNull(PresenceRecord.Decode("not\na\nrecord"));
    }

    [TestMethod]
    public void NamesAreComparedWithoutRegardToCase()
    {
        Assert.AreEqual(PresenceRecord.NameKey("Alice"), PresenceRecord.NameKey(" aLICE "));
    }

    [TestMethod]
    public void AnEnvelopeSurvivesTheWire()
    {
        var origin = Guid.NewGuid();
        var envelope = new CommunityEnvelope(CommunityEnvelopeKind.Event, origin, [4, 5, 6], "friend.changed", new byte[] { 9, 8, 7 });

        Assert.IsTrue(CommunityEnvelope.TryRead(envelope.ToBytes(), out var read));

        Assert.AreEqual(CommunityEnvelopeKind.Event, read.Kind);
        Assert.AreEqual(origin, read.Origin);
        CollectionAssert.AreEqual(new[] { 4, 5, 6 }, read.Targets.ToArray());
        Assert.AreEqual("friend.changed", read.Name);
        CollectionAssert.AreEqual(new byte[] { 9, 8, 7 }, read.Body.ToArray());
    }

    [TestMethod]
    public void NoiseOnTheChannelIsDropped()
    {
        var bytes = new CommunityEnvelope(CommunityEnvelopeKind.Packet, Guid.NewGuid(), [1], string.Empty, new byte[20]).ToBytes();

        Assert.IsFalse(CommunityEnvelope.TryRead(Array.Empty<byte>(), out _));
        Assert.IsFalse(CommunityEnvelope.TryRead(new byte[] { 7, 1, 2, 3 }, out _));
        Assert.IsFalse(CommunityEnvelope.TryRead(bytes.AsMemory(0, bytes.Length - 5), out _));
    }

    [TestMethod]
    public void AMessageReachesAPersonOnAnotherServer()
    {
        var community = new MemoryCommunity();

        using var first = new TestServer(community);
        using var second = new TestServer(community);

        using var sender = first.Join(1, "Alice");
        using var receiver = second.Join(2, "Bob");

        first.Run(() => first.Network.SendAsync(2, new Ping(42)).AsTask());

        var frames = receiver.Read();

        Assert.HasCount(1, frames);
        Assert.AreEqual((byte)GroupOpcode.Chat, frames[0].Group);
        Assert.AreEqual((byte)ChatOpcode.Notify, frames[0].Command);
        Assert.AreEqual(42, ValueOf(frames[0]));

        Assert.IsEmpty(sender.Read());
    }

    [TestMethod]
    public void AMessageReachesPeopleHereAndThere()
    {
        var community = new MemoryCommunity();

        using var first = new TestServer(community);
        using var second = new TestServer(community);

        using var here = first.Join(1, "Alice");
        using var also = first.Join(2, "Bob");
        using var there = second.Join(3, "Carol");
        using var other = second.Join(4, "Dave");

        first.Run(() => first.Network.SendAsync([1, 3, 3, 999], new Ping(7)).AsTask());

        Assert.HasCount(1, here.Read());
        Assert.HasCount(1, there.Read());
        Assert.IsEmpty(also.Read());
        Assert.IsEmpty(other.Read());
    }

    [TestMethod]
    public void ABroadcastReachesEveryoneOnce()
    {
        var community = new MemoryCommunity();

        using var first = new TestServer(community);
        using var second = new TestServer(community);

        using var a = first.Join(1, "Alice");
        using var b = second.Join(2, "Bob");
        using var c = second.Join(3, "Carol");

        first.Run(() => first.Network.BroadcastAsync(new Ping(5)).AsTask());

        Assert.HasCount(1, a.Read());
        Assert.HasCount(1, b.Read());
        Assert.HasCount(1, c.Read());
    }

    [TestMethod]
    public void AnEventRunsOnTheServerOfThePerson()
    {
        var community = new MemoryCommunity();

        using var first = new TestServer(community);
        using var second = new TestServer(community);

        using var a = first.Join(1, "Alice");
        using var b = second.Join(2, "Bob");

        var seen = new List<(string Where, string Name, int[] Persons, byte[] Body)>();

        void Watch(string where, TestServer server)
        {
            server.Network.On("test.event", e =>
            {
                lock (seen)
                {
                    seen.Add((where, e.Name, e.Persons.ToArray(), e.Body.ToArray()));
                }

                return ValueTask.CompletedTask;
            });
        }

        Watch("first", first);
        Watch("second", second);

        first.Run(() => first.Network.PublishAsync([2], "test.event", new byte[] { 1, 2 }).AsTask());

        SpinWait.SpinUntil(() => seen.Count >= 1, 2000);
        Thread.Sleep(200);

        Assert.HasCount(1, seen);
        Assert.AreEqual("second", seen[0].Where);
        CollectionAssert.AreEqual(new[] { 2 }, seen[0].Persons);
        CollectionAssert.AreEqual(new byte[] { 1, 2 }, seen[0].Body);
    }

    [TestMethod]
    public void AnEventForNobodyInParticularRunsOnEveryServer()
    {
        var community = new MemoryCommunity();

        using var first = new TestServer(community);
        using var second = new TestServer(community);

        var count = 0;

        foreach (var server in new[] { first, second })
        {
            server.Network.On("test.all", _ =>
            {
                Interlocked.Increment(ref count);

                return ValueTask.CompletedTask;
            });
        }

        first.Run(() => first.Network.PublishAsync([], "test.all", ReadOnlyMemory<byte>.Empty).AsTask());

        SpinWait.SpinUntil(() => Volatile.Read(ref count) >= 2, 2000);
        Thread.Sleep(200);

        Assert.AreEqual(2, Volatile.Read(ref count));
    }

    [TestMethod]
    public void APersonWhoLeftHearsNothing()
    {
        var community = new MemoryCommunity();

        using var first = new TestServer(community);
        using var second = new TestServer(community);

        using var sender = first.Join(1, "Alice");
        using var gone = second.Join(2, "Bob");

        Assert.IsTrue(community.IsOnline(2));

        second.Leave(gone);

        Assert.IsFalse(community.IsOnline(2));

        first.Run(() => first.Network.SendAsync(2, new Ping(1)).AsTask());

        Assert.IsEmpty(gone.Read());
    }

    [TestMethod]
    public void ALeavingServerDoesNotTakeAwayAPersonWhoMovedOn()
    {
        var community = new MemoryCommunity();

        using var first = new TestServer(community);
        using var second = new TestServer(community);

        using var before = first.Join(1, "Alice");
        using var after = second.Join(1, "Alice");

        // The old connection closes after the new one has been announced.
        first.Leave(before);

        Assert.IsTrue(community.IsOnline(1));

        Assert.AreEqual(second.Instance, community.GetAsync(1).AsTask().GetAwaiter().GetResult()?.Instance);
    }

    [TestMethod]
    public void APersonIsFoundByNameFromAnotherServer()
    {
        var community = new MemoryCommunity();

        using var first = new TestServer(community);
        using var second = new TestServer(community);

        using var bob = second.Join(2, "Bob");

        var found = first.Network.FindByNameAsync("bOB").AsTask().GetAwaiter().GetResult();

        Assert.IsNotNull(found);
        Assert.AreEqual(2, found.Person);
        Assert.AreEqual(second.Instance, found.Instance);
        Assert.IsNull(first.Network.FindByNameAsync("Nobody").AsTask().GetAwaiter().GetResult());
    }
}
