using Redis.OM.Modeling;

namespace OpenWorker.Hotspot.Cache.Types;

/// <summary>
/// A running maze server and how many players it has. The record expires unless the server renews it,
/// so a server that stopped without saying so drops out by itself.
/// </summary>
[Document(StorageType = StorageType.Hash)]
public sealed class MazeCache
{
    [Indexed]
    [RedisIdField]
    public required Guid Guid { get; init; }

    [Indexed]
    public required short Gate { get; init; }

    [RedisField]
    public required int Players { get; init; }

    [RedisField]
    public required string Host { get; init; }

    [RedisField]
    public required short Port { get; init; }
}
