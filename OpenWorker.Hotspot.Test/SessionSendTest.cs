using System.Net;
using System.Net.Sockets;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.SoulWorker.Network;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class SessionSendTest
{
    private readonly struct Filler(byte value, int size) : IResponseHotspotMessage
    {
        public MessageOpcode Opcode => new(GroupOpcode.Move, (byte)MoveOpcode.MoveBt);

        public void Write(ref PacketWriter writer)
        {
            for (var index = 0; index <= size; index++)
            {
                writer.WriteByte(value);
            }
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
    public void FramesFromManyThreadsStayWhole()
    {
        var (server, client, listener) = Connect();

        using (server)
        using (client)
        using (listener)
        {
            var session = new ServerSessionComponent(server);

            const int senders = 8;
            const int framesEach = 200;

            var writing = Task.WhenAll(Enumerable.Range(1, senders).Select(sender => Task.Run(() =>
            {
                for (var frame = 0; frame < framesEach; frame++)
                {
                    session.Send(new Filler((byte)sender, 50 + frame % 500));
                }
            })));

            client.ReceiveTimeout = 5000;

            using var reader = new BinaryReader(client.GetStream());

            for (var received = 0; received < senders * framesEach; received++)
            {
                var header = new MessageHeader(reader);
                var payload = reader.ReadBytes(header.ContentLength);

                MessageEncryption.Exchange(payload.AsSpan(), header.Version);

                Assert.AreEqual((byte)GroupOpcode.Move, payload[0]);

                // Everything after the opcode came from one sender.
                Assert.IsTrue(payload.Skip(3).All(e => e == payload[2]), $"Frame {received} is mixed.");
            }

            writing.GetAwaiter().GetResult();
        }
    }

    [TestMethod]
    public void AClientThatDoesNotReadDoesNotHoldTheSender()
    {
        var (server, client, listener) = Connect();

        using (server)
        using (client)
        using (listener)
        {
            var session = new ServerSessionComponent(server);

            var timer = System.Diagnostics.Stopwatch.StartNew();

            // Much more than the socket buffers hold, and nobody reads it.
            for (var frame = 0; frame < 3000; frame++)
            {
                session.Send(new Filler(1, 4000));
            }

            Assert.IsLessThan(5000, timer.ElapsedMilliseconds);
        }
    }

    [TestMethod]
    public void DisconnectSendsWhatIsAlreadyQueued()
    {
        var (server, client, listener) = Connect();

        using (server)
        using (client)
        using (listener)
        {
            var session = new ServerSessionComponent(server);

            for (var frame = 0; frame < 20; frame++)
            {
                session.Send(new Filler(2, 1000));
            }

            session.Disconnect();

            client.ReceiveTimeout = 5000;

            using var reader = new BinaryReader(client.GetStream());

            for (var received = 0; received < 20; received++)
            {
                var header = new MessageHeader(reader);

                Assert.HasCount(header.ContentLength, reader.ReadBytes(header.ContentLength));
            }

            Assert.AreEqual(0, client.GetStream().Read(new byte[1]), "The connection must close after the last frame.");
        }
    }

    [TestMethod]
    public async Task ClosedCompletesAfterTheQueueIsSentAndTheConnectionIsClosed()
    {
        var (server, client, listener) = Connect();

        using (server)
        using (client)
        using (listener)
        {
            var session = new ServerSessionComponent(server);

            for (var frame = 0; frame < 20; frame++)
            {
                session.Send(new Filler(2, 1000));
            }

            session.Disconnect();

            client.ReceiveTimeout = 5000;

            using var reader = new BinaryReader(client.GetStream());

            // The connection closes after the last frame, and not before the client can have it.
            for (var received = 0; received < 20; received++)
            {
                var header = new MessageHeader(reader);

                Assert.HasCount(header.ContentLength, reader.ReadBytes(header.ContentLength));
            }

            await session.Closed.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.AreEqual(0, client.GetStream().Read(new byte[1]));
        }
    }

    [TestMethod]
    public async Task AbortClosesAConnectionWhoseClientDoesNotRead()
    {
        var (server, client, listener) = Connect();

        using (server)
        using (client)
        using (listener)
        {
            var session = new ServerSessionComponent(server);

            // Much more than the socket buffers hold, and nobody reads it.
            for (var frame = 0; frame < 3000; frame++)
            {
                session.Send(new Filler(1, 4000));
            }

            session.Disconnect();

            await Task.Delay(300);

            Assert.IsFalse(session.Closed.IsCompleted, "The writer waits for the client to take what is queued.");

            session.Abort();

            await session.Closed.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    [TestMethod]
    public void TrySendToAClosedConnectionReportsFailure()
    {
        var (server, client, listener) = Connect();

        using (server)
        using (client)
        using (listener)
        {
            var session = new ServerSessionComponent(server);

            server.Close();

            Assert.IsFalse(session.TrySend(new Filler(1, 10)));
        }
    }
}
