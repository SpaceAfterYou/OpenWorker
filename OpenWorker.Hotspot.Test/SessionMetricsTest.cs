using System.Net;
using System.Net.Sockets;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.SoulWorker.Network;
using OpenWorker.Hotspot.Telemetry;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The connections of a process: how many are open, why each ended (once, under the first reason), what is queued
/// to be written and what has gone out.
/// </summary>
[TestClass]
public sealed class SessionMetricsTest
{
    private readonly struct Filler(byte value, int size) : IResponseHotspotMessage
    {
        public MessageOpcode Opcode => new(GroupOpcode.Move, MoveOpcode.MoveBt);

        public void Write(BinaryWriter writer)
        {
            var filler = new byte[size + 1];

            Array.Fill(filler, value);

            writer.Write(filler);
        }
    }

    private static (TcpClient Server, TcpClient Client, TcpListener Listener) Connect()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);

        listener.Start();

        var client = new TcpClient();

        client.Connect(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);

        return (listener.AcceptTcpClient(), client, listener);
    }

    [TestMethod]
    public void EveryReasonHasATagOfItsOwn()
    {
        // A reason added to the enumeration and not to SessionMetrics.Tag falls back to "server" and fails here.
        var tags = Enum.GetValues<SessionCloseReason>()
            .Where(e => e != SessionCloseReason.None)
            .Select(SessionMetrics.Tag)
            .ToArray();

        CollectionAssert.AllItemsAreUnique(tags);
        Assert.HasCount(5, tags);
        Assert.IsTrue(tags.All(e => e is "client" or "server" or "backpressure" or "timeout" or "malformed"));
    }

    [TestMethod]
    public async Task ASessionIsActiveFromItsStartUntilItsWriterEnds()
    {
        using var metrics = new MetricsHost();
        using var active = metrics.Listen<long>("sessions.active");

        var (server, client, listener) = Connect();

        using (server)
        using (client)
        using (listener)
        {
            var session = new ServerSessionComponent(server, metrics.Get<SessionMetrics>());

            Assert.AreEqual(1, active.Sum());

            session.Disconnect();

            await MetricAssertions.UntilAsync(() => active.Sum() == 0, "The session is still active after it was disconnected.");
        }
    }

    [TestMethod]
    [DataRow(SessionCloseReason.Timeout, "timeout")]
    [DataRow(SessionCloseReason.Malformed, "malformed")]
    [DataRow(SessionCloseReason.Client, "client")]
    [DataRow(SessionCloseReason.Server, "server")]
    public async Task ADisconnectCountsTheSessionClosedUnderItsReason(SessionCloseReason reason, string tag)
    {
        using var metrics = new MetricsHost();
        using var closed = metrics.Listen<long>("sessions.closed");

        var (server, client, listener) = Connect();

        using (server)
        using (client)
        using (listener)
        {
            var session = new ServerSessionComponent(server, metrics.Get<SessionMetrics>());

            session.Disconnect(reason);

            await MetricAssertions.UntilAsync(() => closed.Sum() > 0, "The session was not counted closed.");

            Assert.AreEqual(1, closed.Sum(tag: "reason", value: tag));
            Assert.AreEqual(1, closed.Sum());
        }
    }

    [TestMethod]
    public async Task ADisconnectWithoutAReasonIsTheServers()
    {
        using var metrics = new MetricsHost();
        using var closed = metrics.Listen<long>("sessions.closed");

        var (server, client, listener) = Connect();

        using (server)
        using (client)
        using (listener)
        {
            new ServerSessionComponent(server, metrics.Get<SessionMetrics>()).Disconnect();

            await MetricAssertions.UntilAsync(() => closed.Sum() > 0, "The session was not counted closed.");

            Assert.AreEqual(1, closed.Sum("reason", "server"));
        }
    }

    [TestMethod]
    public async Task ASessionIsCountedClosedOnceUnderItsFirstReason()
    {
        using var metrics = new MetricsHost();
        using var closed = metrics.Listen<long>("sessions.closed");

        var (server, client, listener) = Connect();

        using (server)
        using (client)
        using (listener)
        {
            var session = new ServerSessionComponent(server, metrics.Get<SessionMetrics>());

            // The copies of the component share the connection: the one that disconnects first names the reason.
            var copy = session;

            session.Disconnect(SessionCloseReason.Timeout);
            copy.Disconnect(SessionCloseReason.Malformed);
            session.Disconnect();

            server.Close();
            client.Close();

            await MetricAssertions.UntilAsync(() => closed.Sum() > 0, "The session was not counted closed.");
            await Task.Delay(200);

            Assert.AreEqual(1, closed.Sum());
            Assert.AreEqual(1, closed.Sum("reason", "timeout"));
        }
    }

    [TestMethod]
    public async Task AClientThatDoesNotReadIsClosedForBackpressure()
    {
        using var metrics = new MetricsHost();
        using var active = metrics.Listen<long>("sessions.active");
        using var closed = metrics.Listen<long>("sessions.closed");
        using var queue = metrics.Listen<long>("writer.queue.length");

        var (server, client, listener) = Connect();

        using (server)
        using (client)
        using (listener)
        {
            var session = new ServerSessionComponent(server, metrics.Get<SessionMetrics>());

            // The socket buffers hold a few megabytes and the queue 4096 frames; nobody reads.
            for (var frame = 0; frame < 30_000 && closed.Sum() == 0 && session.TrySend(new Filler(1, 4000)); frame++)
            {
            }

            await MetricAssertions.UntilAsync(() => closed.Sum() > 0, "A queue that is full did not close the session.");
            await Task.Delay(200);

            Assert.AreEqual(1, closed.Sum());
            Assert.AreEqual(1, closed.Sum("reason", "backpressure"));
            Assert.AreEqual(0, active.Sum());
            Assert.AreEqual(0, queue.Sum(), "What was queued and never sent is dropped, and leaves the count.");
        }
    }

    [TestMethod]
    public async Task AClientThatHasGoneIsClosedAsTheClient()
    {
        using var metrics = new MetricsHost();
        using var closed = metrics.Listen<long>("sessions.closed");

        var (server, client, listener) = Connect();

        using (server)
        using (listener)
        {
            var session = new ServerSessionComponent(server, metrics.Get<SessionMetrics>());

            client.Dispose();

            // The first write after the client left may still be taken by the socket; a later one fails.
            for (var attempt = 0; attempt < 500 && closed.Sum() == 0; attempt++)
            {
                session.Send(new Filler(1, 1000));

                await Task.Delay(10);
            }

            await MetricAssertions.UntilAsync(() => closed.Sum() > 0, "A write to a client that has gone did not end the session.");

            Assert.AreEqual(1, closed.Sum());
            Assert.AreEqual(1, closed.Sum("reason", "client"));
        }
    }

    [TestMethod]
    public async Task FramesAndBytesAreCountedWhenTheyAreWritten()
    {
        using var metrics = new MetricsHost();
        using var frames = metrics.Listen<long>("frames.sent");
        using var bytes = metrics.Listen<long>("bytes.sent");

        var (server, client, listener) = Connect();

        using (server)
        using (client)
        using (listener)
        {
            var session = new ServerSessionComponent(server, metrics.Get<SessionMetrics>());

            for (var frame = 0; frame < 20; frame++)
            {
                session.Send(new Filler(2, 100 + frame));
            }

            client.ReceiveTimeout = 5000;

            using var reader = new BinaryReader(client.GetStream());

            var received = 0;

            for (var frame = 0; frame < 20; frame++)
            {
                var header = new MessageHeader(reader);

                reader.ReadBytes(header.ContentLength);

                received += MessageHeader.HeaderSize + header.ContentLength;
            }

            await MetricAssertions.UntilAsync(() => frames.Sum() == 20, "The frames were not counted.");

            Assert.AreEqual(received, bytes.Sum(), "The bytes sent are the bytes the client received, headers included.");
        }
    }

    [TestMethod]
    public async Task WriterQueueGrowsWhileTheClientDoesNotReadAndShrinksWhenItDoes()
    {
        using var metrics = new MetricsHost();
        using var queue = metrics.Listen<long>("writer.queue.length");
        using var active = metrics.Listen<long>("sessions.active");

        var (server, client, listener) = Connect();

        using (server)
        using (client)
        using (listener)
        {
            var session = new ServerSessionComponent(server, metrics.Get<SessionMetrics>());

            const int frames = 400;

            // 24 MB: more than the buffers of both sockets take, and far less than the 4096 frames that close the session.
            for (var frame = 0; frame < frames; frame++)
            {
                Assert.IsTrue(session.TrySend(new Filler(3, 60_000)));
            }

            Assert.IsGreaterThan(100, queue.Sum(), "Frames the socket did not take wait in the queue.");

            client.ReceiveTimeout = 10_000;

            using var reader = new BinaryReader(client.GetStream());

            for (var frame = 0; frame < frames; frame++)
            {
                var header = new MessageHeader(reader);

                reader.ReadBytes(header.ContentLength);
            }

            await MetricAssertions.UntilAsync(() => queue.Sum() == 0, "The queue did not empty once the client read everything.");

            Assert.AreEqual(1, active.Sum(), "The session was never closed.");
        }
    }
}
