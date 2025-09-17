using System.Numerics;

namespace OpenWorker.Domain.Components;

public sealed class CreatureComponent___Old(
    int prototype,
    Vector3 position,
    float rotation,
    int health,
    byte level,
    int waypoint,
    int sector)
{
    public int Prototype { get; } = prototype;
    public Vector3 Position { get; } = position;
    public float Rotation { get; } = rotation;
    public int Health { get; } = health;
    public byte Level { get; } = level;
    public int Waypoint { get; } = waypoint;
    public int Sector { get; } = sector;
}