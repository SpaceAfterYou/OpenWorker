using System.Net;
using System.Net.Sockets;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenWorker.AuthServer.App;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Enums;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Modules.Login.Requests;
using OpenWorker.Hotspot.SoulWorker.Network;

namespace OpenWorker.AuthServer.Test;

[TestClass]
public sealed class TestNextHumanNetworkLogin
{
    [TestMethod]
    public async Task IsRejectedByDefaultWithoutTouchingTheDatabase()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);
        var serverSide = await listener.AcceptTcpClientAsync();

        using var world = World.Create();
        var player = world.Create(new ServerSessionComponent(serverSide));

        // Null dependencies make the test fail if the rejected path reaches them.
        var gameplay = new LoginGameplay(
            null!,
            null!,
            world,
            Options.Create(new AuthOptions()),
            NullLogger<LoginGameplay>.Instance,
            null!);

        using var request = new BinaryReader(new MemoryStream(new byte[2048 + 4096 + 21 + 18]));

        await gameplay.TryJoinAsync(
            new ServiceHandleContext(player, CancellationToken.None),
            new LoginNextHumanNetworkRequest(request));

        var stream = client.GetStream();
        using var reader = new BinaryReader(stream);

        var header = new MessageHeader(reader);
        var payload = reader.ReadBytes(header.ContentLength);
        MessageEncryption.Exchange(payload.AsSpan(), header.Version);

        using var body = new BinaryReader(new MemoryStream(payload));

        body.ReadBytes(2); // Opcode.
        body.ReadInt32(); // Account.
        body.ReadByte(); // Tutorial flag.
        body.ReadBytes(18); // MAC address.
        body.ReadBytes(body.ReadInt16()); // Error text.

        Assert.AreEqual((int)LoginErrorMessageCode.WrongUsernameOrPassword, body.ReadInt32());
        Assert.AreEqual(0, await stream.ReadAsync(new byte[1]), "The connection must be closed.");
    }
}
