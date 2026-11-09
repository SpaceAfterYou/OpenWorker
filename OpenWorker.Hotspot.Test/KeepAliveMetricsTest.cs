using System.Net;
using System.Net.Sockets;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Channel;
using OpenWorker.Server.Common;
using OpenWorker.Extensions;
using OpenWorker.Hotspot.Telemetry;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A client that stopped answering the keep-alives is counted closed for a timeout. Creating entities touches the
/// registry of Arch, which does not take two threads at once.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class KeepAliveMetricsTest
{
    [TestMethod]
    public async Task AClientThatNeverAnswersIsCountedClosedForATimeout()
    {
        using var metrics = new MetricsHost();
        using var closed = metrics.Listen<long>("sessions.closed");

        var listener = new TcpListener(IPAddress.Loopback, 0);

        listener.Start();

        using var client = new TcpClient();

        client.Connect(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);

        using var server = listener.AcceptTcpClient();

        var time = new FakeTimeProvider();
        using var world = World.Create();


        var player = world.Create(
            new ServerSessionComponent(server, metrics.Get<SessionMetrics>()),
            KeepAliveComponent.Start(time.GetTimestamp()));

        var service = new KeepAliveService(world, new WorldLoop(NullLogger<WorldLoop>.Instance), time, NullLogger<KeepAliveService>.Instance);

        // Five keep-alives are sent and missed, and the next check gives up.
        for (var check = 0; check <= KeepAliveService.MaxMissedAnswers; check++)
        {
            time.Advance(KeepAliveService.Frequency);

            service.CheckPlayer(player);
        }

        await MetricAssertions.UntilAsync(() => closed.Sum() > 0, "The silent client was not counted closed.");

        Assert.AreEqual(1, closed.Sum());
        Assert.AreEqual(1, closed.Sum("reason", "timeout"));

        listener.Stop();
    }
}
