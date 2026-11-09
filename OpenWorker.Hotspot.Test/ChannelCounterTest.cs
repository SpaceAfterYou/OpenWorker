using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Channel;
using OpenWorker.Channel.Services;
using OpenWorker.Extensions;
using OpenWorker.Hotspot.Cache.Types;
using Redis.OM;
using Redis.OM.Searching;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The count of a channel in Redis follows the joins and leaves, not a clock. Needs Redis with the search module: set
/// OPENWORKER_TEST_REDIS, for example redis://127.0.0.1:6379. Creating entities registers component types in a registry
/// that does not take two threads at once.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class ChannelCounterTest
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task TheCountIsWrittenOnceTheJoinsAndLeavesOfASecondAreInAndTheLifetimeIsRenewedLessOften()
    {
        var redis = Environment.GetEnvironmentVariable("OPENWORKER_TEST_REDIS");

        if (string.IsNullOrEmpty(redis))
        {
            Assert.Inconclusive("OPENWORKER_TEST_REDIS is not set.");
        }

        var connection = new RedisConnectionProvider(redis);

        await connection.Connection.CreateIndexAsync(typeof(ChannelCache));

        var records = connection.RedisCollection<ChannelCache>(saveState: false);
        var time = new FakeTimeProvider();
        var instance = new InstanceOptions { Identifier = Guid.NewGuid(), Location = 10003 };

        using var world = World.Create();

        var provider = ChannelMembers.Provider();
        var channels = ChannelMembers.Channels(world);
        var channel = channels[0];

        // What the bootstrap writes when the server starts.
        await records.InsertAsync(new ChannelCache
        {
            Guid = channel.Record,
            Owner = instance.Identifier,
            Gate = 1,
            Location = instance.Location,
            Identifier = channel.Identifier,
            OnlineCount = 0
        }, ChannelLifecycleService.Lifetime);

        var service = new ChannelLifecycleService(
            records,
            channels,
            Options.Create(instance),
            Options.Create(new GateOptions { Id = 1 }),
            time,
            NullLogger<ChannelLifecycleService>.Instance);

        await service.StartAsync(TestContext.CancellationToken);

        try
        {
            var first = ChannelMembers.Create(world, provider, new RecordingConnection(), 0);
            var second = ChannelMembers.Create(world, provider, new RecordingConnection(), 1);

            channels.Join(0, first);
            channels.Join(0, second);
            await channels.OnSessionClosedAsync(first);

            // The service waits for the second to pass on its clock: nothing is written in real time meanwhile.
            await Task.Delay(200, TestContext.CancellationToken);

            Assert.AreEqual(0, await CountAsync(records, channel), "Nothing is written before the joins and leaves of a second are in.");

            await AdvanceUntilAsync(time, async () => await CountAsync(records, channel) == 1);

            // Nothing changes in the channel: the record is not written again, however long it waits.
            await SetCountAsync(records, channel, instance, 99);

            for (var tick = 0; tick < 5; tick++)
            {
                time.Advance(TimeSpan.FromSeconds(1));

                await Task.Delay(20, TestContext.CancellationToken);
            }

            // Longer than the period of a poll on the real clock, too.
            await Task.Delay(1200, TestContext.CancellationToken);

            Assert.AreEqual(99, await CountAsync(records, channel), "Without a join or a leave the count is not written.");

            // Until the renewal: every record is written again with its lifetime, changed or not (the value written behind
            // the back of the service has none).
            await AdvanceUntilAsync(time, async () => await CountAsync(records, channel) == 1);

            Assert.IsGreaterThan(0d, await TimeToLiveAsync(connection, channel), "The renewal gives the record its lifetime again.");

            channels.Join(0, ChannelMembers.Create(world, provider, new RecordingConnection(), 2));

            await AdvanceUntilAsync(time, async () => await CountAsync(records, channel) == 2);
        }

        finally
        {
            await service.WithdrawAsync(TestContext.CancellationToken);
        }

        Assert.IsNull(await records.FindByIdAsync(channel.Record.ToString()), "The drain takes the record back.");
    }

    private static async Task<short> CountAsync(IRedisCollection<ChannelCache> records, ServiceChannel channel)
    {
        var record = await records.FindByIdAsync(channel.Record.ToString());

        return record?.OnlineCount ?? -1;
    }

    private static async Task<double> TimeToLiveAsync(RedisConnectionProvider connection, ServiceChannel channel)
    {
        var result = await connection.Connection.ExecuteAsync("TTL", $"{typeof(ChannelCache).FullName}:{channel.Record}");

        return result;
    }

    private static Task SetCountAsync(IRedisCollection<ChannelCache> records, ServiceChannel channel, InstanceOptions instance, short count)
    {
        return records.UpdateAsync(new ChannelCache
        {
            Guid = channel.Record,
            Owner = instance.Identifier,
            Gate = 1,
            Location = instance.Location,
            Identifier = channel.Identifier,
            OnlineCount = count
        });
    }

    private static async Task AdvanceUntilAsync(FakeTimeProvider time, Func<Task<bool>> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);

        while (!await condition())
        {
            Assert.IsLessThan(deadline, DateTime.UtcNow, "The count was not written in time.");

            time.Advance(ChannelLifecycleService.Debounce);

            await Task.Delay(20);
        }
    }
}
