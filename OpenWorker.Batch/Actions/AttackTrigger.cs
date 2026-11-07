using System.Numerics;

namespace OpenWorker.Batch.Actions;

/// <summary>
/// The moment of an animation when an attack is judged, and the space it covers.
/// </summary>
public readonly record struct AttackTrigger(
    short RangeType,
    short AttackType,
    int SpawnObject,
    float Start,
    float Angle,
    Vector3 Center,
    float BoxX,
    float BoxY,
    float Radius,
    float Height,
    float StartPosition)
{
    private const short Melee = 0;
    private const short Chain = 5;

    public const short BoxRange = 1;

    /// <summary>
    /// Other types leave it to an object they spawn: a projectile or a trap.
    /// </summary>
    public bool HitsArea => AttackType is Melee or Chain;
}
