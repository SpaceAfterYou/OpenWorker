using Redis.OM.Modeling;

namespace OpenWorker.Hotspot.Cache.Types;

[Document(StorageType = StorageType.Hash)]
public sealed class SessionCache
{
    [RedisIdField]
    [Indexed]
    public required long Session { get; init; }

    [Indexed]
    public required int Account { get; init; }

    [Indexed]
    public required DateTime UpdatedAt { get; init; }
}