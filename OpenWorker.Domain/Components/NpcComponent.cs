using System.Numerics;

namespace OpenWorker.Domain.Components;

/// <summary>
/// An NPC a district map places where it stands, as the client lists it when a player enters.
/// </summary>
public sealed class NpcComponent(
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