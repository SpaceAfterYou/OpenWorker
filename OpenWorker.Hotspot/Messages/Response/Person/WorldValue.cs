using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;

namespace OpenWorker.Hotspot.Messages.Response.Person;

public readonly struct WorldValue(WorldComponent component)
{
    public short Location { get; init; } = component.Location;
    public MapValue Map { get; init; } = component.Map;
    public Vector3 Position { get; init; } = component.Position;
    public float Rotation { get; init; } = component.Rotation;

    public WorldValue(Entity entity) : this(entity.Get<WorldComponent>())
    {
    }
    
    public WorldValue(Arch.Core.World world, Entity player) : this(world.Get<WorldComponent>(player))
    {
    }
    
    public static implicit operator WorldValue(WorldComponent component)
    {
        return new WorldValue(component);
    }
}