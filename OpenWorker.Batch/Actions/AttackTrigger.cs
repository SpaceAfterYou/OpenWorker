using System.Numerics;

namespace OpenWorker.Batch.Actions;

/// <summary>
/// The moment of an animation when an attack is judged, and the space it covers.
/// </summary>
/// <param name="Id">How the client names this attack in packets.</param>
public readonly record struct AttackTrigger(
    int Id,
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
    float StartPosition,
    float DamageRate = 1f,
    int ReactionType = 1,
    ProjectileInfo? Projectile = null)
{
    private const short Melee = 0;
    private const short Thrown = 1;
    private const short Trap = 2;
    private const short Chain = 5;

    public const short FromBody = 6;
    public const short BoxRange = 1;

    /// <summary>
    /// Other types leave it to an object they spawn: a projectile or a trap.
    /// </summary>
    public bool HitsArea => AttackType is Melee or Chain;

    public bool IsProjectile => AttackType is Thrown or FromBody;

    /// <summary>
    /// Stays where it was placed and hits whoever is around.
    /// </summary>
    public bool IsTrap => AttackType is Trap;

    /// <summary>
    /// Some attacks only show something and affect nobody.
    /// </summary>
    public bool AffectsTargets => DamageRate > 0f || ReactionType != 0;
}

/// <summary>
/// How an attack flies. Angles are in degrees: X turns it, Y lowers it.
/// </summary>
public sealed record ProjectileInfo(
    Vector3 Start,
    float Speed,
    float LifeTime,
    bool HasGravity,
    int DirectionType,
    float Radius,
    bool IsContinuous,
    float ContinuousTime,
    bool IsGuided,
    bool TargetOnly,
    short ExplodeDecision,
    Vector3 Direction,
    bool Penetrates,
    int PenetrateCount)
{
    public const int ByCamera = 0;
    public const int AlongGround = 2;
}
