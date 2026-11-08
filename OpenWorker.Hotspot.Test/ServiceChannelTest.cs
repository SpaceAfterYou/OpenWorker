using Arch.Core;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// Creating entities registers component types in a registry that does not take two threads at once.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class ServiceChannelTest
{
    /// <summary>
    /// The members of a channel belong to the world loop, and the services that publish the count to Redis read it from
    /// a thread of the pool.
    /// </summary>
    [TestMethod]
    public async Task AReaderOffTheLoopSeesTheCountOfJoinsAndLeaves()
    {
        var provider = ChannelMembers.Provider();

        using var world = World.Create();

        var channels = ChannelMembers.Channels(world);

        var first = ChannelMembers.Create(world, provider, new RecordingConnection(), 0);
        var second = ChannelMembers.Create(world, provider, new RecordingConnection(), 1);

        channels.Join(0, first);
        channels.Join(0, second);

        Assert.AreEqual(2, await Task.Run(() => channels[0].Online));

        await channels.OnSessionClosedAsync(first);

        Assert.AreEqual(1, await Task.Run(() => channels[0].Online));
        Assert.IsFalse(channels[0].Contains(first));
    }
}
