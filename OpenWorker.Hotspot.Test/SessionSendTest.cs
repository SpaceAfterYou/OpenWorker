using System.Net;
using System.Net.Sockets;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.SoulWorker.Network;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class SessionSendTest
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
