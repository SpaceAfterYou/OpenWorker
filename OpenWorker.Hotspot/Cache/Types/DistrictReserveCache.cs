using Redis.OM.Modeling;

namespace OpenWorker.Hotspot.Cache.Types;

[Document(StorageType = StorageType.Hash)]
public sealed class DistrictReserveCache
{
    [RedisIdField]
    public required int Account { get; init; }
    
    [RedisField]
    public required int Person { get; init; }
    
    [RedisField]
    public required Guid Channel { get; init; }
    
    [Indexed]
    public required Guid District { get; init; }
    
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

    /// <summary>
    /// Whether the person may enter that district with this reservation. The reservation belongs to the account,
    /// and the district that wrote it is the only one it is good for.
    /// </summary>
    public bool Admits(int person, Guid district)
    {
        return Person == person && District == district;
    }
}