using System.Numerics;

namespace OpenWorker.Batch.Actions;

/// <summary>
/// The moment of an animation when an attack is judged, and the space it covers.
/// </summary>
/// <param name="Id">How the client names this attack in packets.</param>
/// <param name="Buff">The buff the attack gives every target it reaches, zero for none.</param>
/// <param name="TargetType">Whom the attack is for: the enemies of the attacker, the attacker itself, its friends or its party.</param>
/// <param name="Aura">The aura the attack puts on the targets it reaches (they give its buff to those around them), zero for none.</param>
/// <param name="Reaction">How hard the attack throws its targets.</param>
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
    ProjectileInfo? Projectile = null,
    ChainInfo? Chain = null,
    int Buff = 0,
    int TargetType = AttackTrigger.Enemies,
    int Aura = 0,
    AttackReaction Reaction = default)
{
    /// <summary>
    /// The attacker itself, wherever it stands.
    /// </summary>
    public const int Self = 1;

    /// <summary>
    /// The attacker and its friends in the area.
    /// </summary>
    public const int SelfAndFriends = 2;

    public const int Enemies = 3;

    /// <summary>
    /// The attacker and its party in the area.
    /// </summary>
    public const int SelfAndParty = 7;

    /// <summary>
    /// The party of the attacker in the area, without the attacker.
    /// </summary>
    public const int Party = 8;

    /// <summary>
    /// The friends of the attacker in the area, without the attacker.
    /// </summary>
    public const int Friends = 6;

    private const short Melee = 0;
    private const short Thrown = 1;
    private const short Trap = 2;
    private const short Chained = 5;

    public const short FromBody = 6;
    public const short BoxRange = 1;

    /// <summary>
    /// Other types leave it to an object they spawn: a projectile, a trap or a chain.
    /// </summary>
    public bool HitsArea => AttackType is Melee;

    public bool IsProjectile => AttackType is Thrown or FromBody;

    /// <summary>
    /// Stays where it was placed and hits whoever is around.
    /// </summary>
    public bool IsTrap => AttackType is Trap;

    /// <summary>
    /// Jumps from one target to the next one close to it.
    /// </summary>
    public bool IsChain => AttackType is Chained;

    /// <summary>
    /// Some attacks only show something and affect nobody.
    /// </summary>
    public bool AffectsTargets => DamageRate > 0f || ReactionType != 0 || Buff > 0 || Aura > 0;

    /// <summary>
    /// Whether the attack is for the side of the attacker (itself, its friends, its party) rather than its enemies.
    /// </summary>
    public bool ForAllies => TargetType is Self or SelfAndFriends or Friends or SelfAndParty or Party;

    /// <summary>
    /// Whether the attacker is among the targets of an attack for its side.
    /// </summary>
    public bool IncludesAttacker => TargetType is Self or SelfAndFriends or SelfAndParty;
}

/// <summary>
/// How the targets of an attack are thrown by it, as the event of the attack says. Distances are in the units of the world,
/// speeds in units a second, times in seconds, angles in degrees.
/// </summary>
/// <param name="Distance">How far the blow pushes a target along the way it looks; a negative one draws the target in.</param>
/// <param name="Height">How high it lifts a target that stands on the ground.</param>
/// <param name="HeightInAir">How high it lifts a target that is already in the air.</param>
/// <param name="Speed">How fast a target flies; zero for the distance it has to cover.</param>
/// <param name="Arrow">Which way the target goes: <see cref="AttackReaction.AlongAttacker"/>, <see cref="AttackReaction.AwayFromOrigin"/> or <see cref="AttackReaction.StraightAlongAttacker"/>; any other value pushes along the attacker without a turn.</param>
/// <param name="Angle">How far the push is turned from the way the attacker looks.</param>
/// <param name="SlowRate">How much of its speed a weak blow leaves to the target.</param>
/// <param name="SlowTime">How long a blow slows the target.</param>
/// <param name="SlowDelay">How long before the slow begins.</param>
/// <param name="HitFreeze">How long the blow stops the animation of the attacker.</param>
/// <param name="WeakSlowChance">The chance in percent that a weak blow slows the target.</param>
/// <param name="Grade">How strong a blow it is against the rank of the target: a target of a higher rank takes it as a plain flinch.</param>
/// <param name="UsesTargetWeight">The weight of the target scales the push.</param>
/// <param name="Hardness">0 is a weak blow that shows no reaction, 1 a plain one, 3 and 4 strong ones that break a super armor.</param>
/// <param name="BreaksPlayerArmor">The blow breaks the super armor of a player.</param>
public readonly record struct AttackReaction(
    float Distance,
    float Height,
    float HeightInAir,
    float Speed,
    int Arrow,
    float Angle,
    float SlowRate,
    float SlowTime,
    float SlowDelay,
    float HitFreeze,
    short WeakSlowChance,
    int Grade,
    bool UsesTargetWeight,
    short Hardness,
    bool BreaksPlayerArmor)
{
    /// <summary>
    /// The push is turned by the way the attacker looks, a little at random.
    /// </summary>
    public const int AlongAttacker = 1;

    /// <summary>
    /// The push goes away from the place the blow comes from.
    /// </summary>
    public const int AwayFromOrigin = 2;

    /// <summary>
    /// The push goes exactly the way the attacker looks, and always as far as the distance says.
    /// </summary>
    public const int StraightAlongAttacker = 3;

    /// <summary>
    /// Whether the blow moves its target at all.
    /// </summary>
    public bool Moves => Hardness != 0 || Distance > 0f || Height > 0f;
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

/// <summary>
/// How a chain finds its next target and how long it lives.
/// </summary>
/// <param name="ConnectionType">0 takes one target after another, 1 takes the first and then the rest from it, 2 takes them all at once.</param>
/// <param name="Radius">How far from the last target the next one may stand.</param>
/// <param name="DamageMultiple">The share of the damage for every target after the first.</param>
/// <param name="RecalculatesTargets">Whether every repeated hit looks for the targets again.</param>
public sealed record ChainInfo(
    int ConnectionType,
    float LifeTime,
    int MaxCount,
    float DamageMultiple,
    Vector3 Center,
    float Radius,
    float StartPosition,
    bool RecalculatesTargets)
{
    public const int OneByOne = 0;
    public const int FromFirst = 1;
    public const int AllAtOnce = 2;
}
