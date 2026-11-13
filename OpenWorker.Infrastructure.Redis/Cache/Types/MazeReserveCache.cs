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

    /// <summary>
    /// The instance the person joins, the one the party shares; empty for a person alone, who gets a maze of their own.
    /// </summary>
    [RedisField]
    public Guid Instance { get; set; }

    /// <summary>
    /// The party the person enters with, or zero.
    /// </summary>
    [RedisField]
    public int Party { get; set; }

    /// <summary>
    /// How many members of the party the maze was opened for, which decides how strong its monsters are; zero for a person alone.
    /// </summary>
    [RedisField]
    public byte Members { get; set; }

    /// <summary>
    /// The channel of the open maze of the world the person goes into, or zero for any other maze.
    /// </summary>
    [RedisField]
    public byte World { get; set; }

    /// <summary>
    /// The account whose room the maze is, or zero for a maze that is no room.
    /// </summary>
    [RedisField]
    public int Room { get; set; }

    /// <summary>
    /// The district the person goes back to when it leaves the room.
    /// </summary>
    [RedisField]
    public short Return { get; set; }

    /// <summary>
    /// The room is of a friend of the person: going into it counts for the achievement of visits once the room lets it in.
    /// </summary>
    [RedisField]
    public bool Friend { get; set; }
}