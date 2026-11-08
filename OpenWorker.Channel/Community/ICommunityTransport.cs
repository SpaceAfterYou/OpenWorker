namespace OpenWorker.Channel.Community;

/// <summary>
/// Carries messages between the servers. A message goes to every server that listens to the channel, in no particular
/// order relative to other channels.
/// </summary>
public interface ICommunityTransport
{
    ValueTask PublishAsync(string channel, ReadOnlyMemory<byte> payload);

    /// <summary>
    /// The handler is called on a thread of the transport, so it must hand the message over to the world loop.
    /// </summary>
    ValueTask SubscribeAsync(string channel, Action<ReadOnlyMemory<byte>> handler);
}

/// <summary>
/// Who is online and where. Entries expire, so that a server that is gone does not keep its players for ever.
/// </summary>
public interface IPresenceStore
{
    ValueTask SetAsync(PresenceRecord record, TimeSpan lifetime);

    /// <summary>
    /// Removes the entry only while it still belongs to the instance: the person may already have reconnected to another server.
    /// </summary>
    ValueTask RemoveAsync(PresenceRecord record);

    ValueTask<PresenceRecord?> GetAsync(int person);

    ValueTask<PresenceRecord?> GetByNameAsync(string name);

    ValueTask<IReadOnlyList<PresenceRecord?>> GetManyAsync(IReadOnlyList<int> persons);
}
