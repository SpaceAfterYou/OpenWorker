using System.Numerics;

namespace OpenWorker.Hotspot.Messages.Response.Person;

public readonly struct WorldValue
{
    public short Location { get; init; }
    public MapValue Map { get; init; }
    public Vector3 Position { get; init; }
    public float Rotation { get; init; }

    public WorldValue(short location, MapValue map, Vector3 position, float rotation)
    {
        Location = location;
        Map = map;
        Position = position;
        Rotation = rotation;
    }
}
