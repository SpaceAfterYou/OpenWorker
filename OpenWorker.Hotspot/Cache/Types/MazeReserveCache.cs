using Redis.OM.Modeling;

namespace OpenWorker.Hotspot.Cache.Types;

[Document(StorageType = StorageType.Hash)]
public sealed class MazeReserveCache
{
    [Indexed]

    [RedisIdField]
    public required int Person { get; set; }

    [RedisField]
    public required int Account { get; init; }
    
    [RedisField]
    public required short Location { get; set; }
    
    [RedisField]
    public required float X { get; set; }
    
    [RedisField]
    public required float Y { get; set; }
    
    [RedisField]
    public required float Z { get; set; }
    
    [RedisField]
    public required float R { get; set; }
    
    [RedisField]
    public required int Jump { get; set; }
}