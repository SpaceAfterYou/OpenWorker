using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The accept and read loops of a server over a real socket: what they count, and why a connection that ends by itself ends.
/// They create entities in the registry of Arch, which does not take two threads at once, and use <see cref="ProbeHandler"/>.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class ServerServiceMetricsTest
{
    [TestMethod]
    public async Task AClientThatLeavesIsCountedAsTheClientAndWhatItSentIsCounted()
    {
        await using var server = new ServerServiceHost();
        using var active = server.Metrics.Listen<long>("sessions.active");
        using var closed = server.Metrics.Listen<long>("sessions.closed");
        using var received = server.Metrics.Listen<long>("bytes.received");

        await server.StartAsync();

        using var client = await server.ConnectAsync();

        await MetricAssertions.UntilAsync(() => active.Sum() == 1, "The connection was not counted.");

        var frame = ServerServiceHost.Frame((byte)GroupOpcode.System, (byte)ProbeRequest.Command, ProbeRequest.Fine);

        await client.GetStream().WriteAsync(frame);

        await MetricAssertions.UntilAsync(() => ProbeHandler.Handled == 1, "The packet did not reach its handler.");

        Assert.AreEqual(frame.Length, received.Sum(), "The header counts.");

        client.Dispose();

        await MetricAssertions.UntilAsync(() => closed.Sum() > 0, "A client that left was not counted.");

        Assert.AreEqual(1, closed.Sum("reason", "client"));
        Assert.AreEqual(1, closed.Sum());
        await MetricAssertions.UntilAsync(() => active.Sum() == 0, "The connection stayed active after it closed.");
    }

    [TestMethod]
    public async Task AClientThatSendsTooManyBadPacketsIsCountedAsMalformedNotAsTheClient()
    {
        await using var server = new ServerServiceHost();
        using var closed = server.Metrics.Listen<long>("sessions.closed");
        using var errors = server.Metrics.Listen<long>("handler.errors");

        await server.StartAsync();

        using var client = await server.ConnectAsync();

        // The mode byte is missing: the request ends the stream.
        var bad = ServerServiceHost.Frame((byte)GroupOpcode.System, (byte)ProbeRequest.Command);

        for (var sent = 0; sent < 10; sent++)
        {
            await client.GetStream().WriteAsync(bad);
        }

        await MetricAssertions.UntilAsync(() => closed.Sum() > 0, "The server did not close the connection.");

        // The client sees the close and its socket reports the end of the stream, which must not become a second count.
        client.Dispose();
        await Task.Delay(200);

        Assert.AreEqual(1, closed.Sum());
        Assert.AreEqual(1, closed.Sum("reason", "malformed"));
        Assert.AreEqual(10, errors.Sum("reason", "malformed"));
    }

    [TestMethod]
    public async Task AConnectionThatSendsNothingIsClosedWhenItsTimeToEnterRunsOutAndOneThatEnteredStays()
    {
        await using var server = new ServerServiceHost();
        using var active = server.Metrics.Listen<long>("sessions.active");
        using var closed = server.Metrics.Listen<long>("sessions.closed");

        await server.StartAsync();

        using var player = await server.ConnectAsync();

        // The entity of a connection is made a moment after the connection is accepted.
        var deadline = DateTime.UtcNow.AddSeconds(10);

        while (await server.EnterAllAsync(account: 7) == 0)
        {
            Assert.IsLessThan(deadline, DateTime.UtcNow, "The connection did not get its entity.");

            await Task.Delay(20);
        }

        using var silent = await server.ConnectAsync();

        await MetricAssertions.UntilAsync(() => active.Sum() == 2, "The second connection was not counted.");

        // Each connection starts its clock when its read loop does: the clock is moved on until the silent one is closed.
        await MetricAssertions.UntilAsync(() =>
        {
            server.Time.Advance(TimeSpan.FromSeconds(30));

            return closed.Sum() > 0;
        }, "A connection that never entered was not closed.");

        var buffer = new byte[1];
        using var read = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        Assert.AreEqual(0, await silent.GetStream().ReadAsync(buffer, read.Token), "The server did not end the stream of the silent connection.");

        // The player is past the time to enter, more than once, and stays.
        server.Time.Advance(TimeSpan.FromMinutes(5));
        await Task.Delay(200);

        Assert.AreEqual(1, closed.Sum("reason", "no_entry"));
        Assert.AreEqual(1, closed.Sum());
        Assert.AreEqual(1, active.Sum());
    }

    [TestMethod]
    public async Task AConnectionThatTheServerEndsByStoppingIsCountedAsTheShutdown()
    {
        await using var server = new ServerServiceHost();
        using var active = server.Metrics.Listen<long>("sessions.active");
        using var closed = server.Metrics.Listen<long>("sessions.closed");

        await server.StartAsync();

        using var client = await server.ConnectAsync();

        await MetricAssertions.UntilAsync(() => active.Sum() == 1, "The connection was not counted.");

        await server.StopAsync();

        await MetricAssertions.UntilAsync(() => closed.Sum() > 0, "The connection was not counted closed when the server stopped.");

        // The stop cancels the reading, as the drain does: the client did not leave, and the server did not fail.
        Assert.AreEqual(1, closed.Sum("reason", "shutdown"));
        Assert.AreEqual(1, closed.Sum());
    }
}
