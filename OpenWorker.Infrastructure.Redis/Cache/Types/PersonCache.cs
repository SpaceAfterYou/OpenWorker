using Redis.OM.Modeling;

namespace OpenWorker.Hotspot.Cache.Types;

[Document(StorageType = StorageType.Hash)]
public sealed class PersonCache
{
    [Indexed]
    [RedisIdField]
    public required int Person { get; init; }

    [Indexed]
    public required short World { get; init; }

    [Indexed]
    public required float X { get; init; }

    [Indexed]
    public required float Y { get; init; }

    [Indexed]
    public required float Z { get; init; }

    [Indexed]
    public required DateTime UpdatedAt { get; init; }
}