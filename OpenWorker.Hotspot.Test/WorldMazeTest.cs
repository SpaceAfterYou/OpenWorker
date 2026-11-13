using OpenWorker.Extensions;
using OpenWorker.Hotspot.Cache;
using StackExchange.Redis;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The open maze of the world (District 6): everybody of a channel shares one instance. The Redis rule needs Redis: set
/// OPENWORKER_TEST_REDIS.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class WorldMazeTest
{
    private const short Gate = 1;
    private const short District6 = 30021;

    private static readonly (Guid Server, string Host, short Port) Unregistered = (Guid.Empty, "maze", 100);

    private static ConnectionMultiplexer? _redis;

    private static async Task<IConnectionMultiplexer> RedisAsync()
    {
        var connection = Environment.GetEnvironmentVariable("OPENWORKER_TEST_REDIS");

        if (string.IsNullOrEmpty(connection))
        {
            Assert.Inconclusive("OPENWORKER_TEST_REDIS is not set.");
        }

        return _redis ??= await ConnectionMultiplexer.ConnectAsync(new RedisOptions { ConnectionString = connection }.ToConfigurationOptions());
    }

    [ClassCleanup]
    public static void Cleanup()
    {
        _redis?.Dispose();
        _redis = null;
    }

    /// <summary>
    /// The first player of a channel opens the instance, the next ones of the channel are sent into it until it is full; the
    /// record follows the instance that holds it, and the next player after it ended opens a new one.
    /// </summary>
    [TestMethod]
    public async Task EverybodyOfAChannelIsSentIntoOneInstanceUntilItIsFull()
    {
        var redis = await RedisAsync();
        var directory = new WorldMazeDirectory(redis);
        var channel = PartyTest.People(1)[0];

        var first = await directory.RouteAsync(Gate, District6, channel, Unregistered, capacity: 2);
        var second = await directory.RouteAsync(Gate, District6, channel, Unregistered, capacity: 2);

        Assert.IsNotNull(first);
        Assert.AreEqual(first.Value.Instance, second?.Instance, "the second player of the channel is sent where the first is");
        Assert.AreEqual(2, second?.Players);
        Assert.IsNull(await directory.RouteAsync(Gate, District6, channel, Unregistered, capacity: 2), "a full instance takes nobody");

        var other = await directory.RouteAsync(Gate, District6, channel + 1, Unregistered, capacity: 2);

        Assert.AreNotEqual(first.Value.Instance, other?.Instance, "another channel has an instance of its own");

        Assert.IsFalse(await directory.HoldAsync(Gate, District6, channel, Guid.NewGuid(), Unregistered, 0), "another instance does not take the record");
        Assert.IsTrue(await directory.HoldAsync(Gate, District6, channel, first.Value.Instance, Unregistered, 1), "the instance renews its record");
        Assert.AreEqual(first.Value.Instance, (await directory.RouteAsync(Gate, District6, channel, Unregistered, capacity: 2))?.Instance,
            "the renewed count lets one more in");

        await directory.ReleaseAsync(Gate, District6, channel, Guid.NewGuid());

        Assert.IsNull(await directory.RouteAsync(Gate, District6, channel, Unregistered, capacity: 2),
            "the release of another instance leaves the record, full again");

        await directory.ReleaseAsync(Gate, District6, channel, first.Value.Instance);

        Assert.AreNotEqual(first.Value.Instance, (await directory.RouteAsync(Gate, District6, channel, Unregistered, capacity: 2))?.Instance,
            "the next player after the instance ended opens a new one");

        await directory.ReleaseAsync(Gate, District6, channel + 1, other!.Value.Instance);
    }
}
