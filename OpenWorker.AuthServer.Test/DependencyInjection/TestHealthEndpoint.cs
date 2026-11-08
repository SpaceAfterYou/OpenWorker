using System.Net;
using System.Text.Json;

namespace OpenWorker.AuthServer.Test.DependencyInjection;

/// <summary>
/// The health endpoint of a real host over HTTP, the way <c>curl</c> in the container sees it.
/// </summary>
[TestClass]
public sealed class TestHealthEndpoint
{
    private static async Task<(HttpStatusCode Status, JsonElement Body)> GetAsync(ServerHarness server, string path, CancellationToken cancellationToken)
    {
        using var response = await server.Http.GetAsync(path, cancellationToken);

        var text = await response.Content.ReadAsStringAsync(cancellationToken);

        return (response.StatusCode, JsonDocument.Parse(text).RootElement.Clone());
    }

    [TestMethod]
    public async Task ServerThatListensIsAliveAndReady()
    {
        await using var server = await ServerHarness.StartAsync(cancellationToken: TestContext.CancellationToken);

        var (live, liveBody) = await GetAsync(server, "/healthz", TestContext.CancellationToken);
        var (ready, readyBody) = await GetAsync(server, "/readyz", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, live);
        Assert.AreEqual("Healthy", liveBody.GetProperty("status").GetString());
        Assert.AreEqual("Healthy", liveBody.GetProperty("checks").GetProperty("world-loop").GetProperty("status").GetString());

        // The listener is no business of liveness: a server that does not listen yet is alive.
        Assert.IsFalse(liveBody.GetProperty("checks").TryGetProperty("listener", out _));

        Assert.AreEqual(HttpStatusCode.OK, ready);
        Assert.AreEqual("Healthy", readyBody.GetProperty("checks").GetProperty("listener").GetProperty("status").GetString());
        Assert.AreEqual("Healthy", readyBody.GetProperty("checks").GetProperty("world-loop").GetProperty("status").GetString());
    }

    [TestMethod]
    public async Task EverythingElseIsNotFound()
    {
        await using var server = await ServerHarness.StartAsync(cancellationToken: TestContext.CancellationToken);

        using var response = await server.Http.GetAsync("/", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task StuckLoopFailsLivenessAndReadinessUntilItRecovers()
    {
        await using var server = await ServerHarness.StartAsync(
            settings: new() { ["Health:LoopTimeout"] = "00:00:00.300" },
            cancellationToken: TestContext.CancellationToken);

        using var release = new ManualResetEventSlim();

        var stuck = server.Loop.InvokeAsync(() => release.Wait(TestContext.CancellationToken));

        var (live, liveBody) = await GetAsync(server, "/healthz", TestContext.CancellationToken);
        var (ready, _) = await GetAsync(server, "/readyz", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, live);
        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, ready);
        StringAssert.Contains(liveBody.GetProperty("checks").GetProperty("world-loop").GetProperty("description").GetString(), "world loop");

        release.Set();

        await stuck;

        var (recovered, _) = await GetAsync(server, "/healthz", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, recovered);
    }

    public TestContext TestContext { get; set; } = null!;
}
