using Redis.OM.Modeling;

namespace OpenWorker.Hotspot.Cache.Types;

/// <summary>
/// The maze a party plays together: which maze server runs it and which instance on it, so that a member who comes later,
/// from any district and channel, is sent into the same one. The district that sends the party in writes it; the maze
/// server takes it back when the last member leaves the instance. It also expires, in case the maze server stops.
/// </summary>
[Document(StorageType = StorageType.Hash)]
public sealed class PartyMazeCache
{
    /// <summary>
    /// How long the record of a party maze lives at most without its maze server taking it back.
    /// </summary>
    public static TimeSpan Lifetime { get; } = TimeSpan.FromHours(6);

    // The index of the relay needs a field to be made; the record is only ever read by its key.
    [Indexed]
    [RedisIdField]
    public required int Party { get; init; }

    /// <summary>
    /// The maze location the party plays.
    /// </summary>
    [RedisField]
    public required short Location { get; init; }

    /// <summary>
    /// The instance on the maze server that the members share.
    /// </summary>
    [RedisField]
    public required Guid Instance { get; init; }

    /// <summary>
    /// The identifier of the maze server, which names its record (<see cref="MazeCache"/>): without the record the server is gone.
    /// </summary>
    [RedisField]
    public required Guid Server { get; init; }

    [RedisField]
    public required string Host { get; init; }

    [RedisField]
    public required short Port { get; init; }

    /// <summary>
    /// How many members the maze was opened for.
    /// </summary>
    [RedisField]
    public byte Members { get; init; }
}
