using Redis.OM.Modeling;

namespace OpenWorker.Hotspot.Cache.Types;

[Document(StorageType = StorageType.Hash)]
public sealed class DistrictCache
{
    [Indexed]
    [RedisIdField]
    public required Guid Guid { get; init; }

    [Indexed]
    public required short Gate { get; init; }

    [Indexed]
    public required short Location { get; init; }

    [Indexed]
    public required short Group { get; init; }

    [RedisField]
    public required float PositionX { get; init; }

    [RedisField]
    public required float PositionY { get; init; }

    [RedisField]
    public required float PositionZ { get; init; }

    [RedisField]
    public required float Rotation { get; init; }

    [RedisField]
    public required int Jump { get; init; }

    [RedisField]
    public required int Portal { get; init; }

    [RedisField]
    public required string Host { get; init; }

    [RedisField]
    public required short Port { get; init; }
}