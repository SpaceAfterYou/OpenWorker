using Redis.OM.Modeling;

namespace OpenWorker.Hotspot.Cache.Types;

[Document(StorageType = StorageType.Hash)]
public sealed class LocationCache
{
    [Indexed]
    public short Identifier { get; set; }

    [Indexed]
    [RedisIdField]
    public int Person { get; set; }

    [RedisField]
    public required float PositionX { get; set; }

    [RedisField]
    public required float PositionY { get; set; }

    [RedisField]
    public required float PositionZ { get; set; }

    [RedisField]
    public required float RotationX { get; set; }

    [RedisField]
    public required float RotationY { get; set; }

    [RedisField]
    public required float RotationZ { get; set; }
}