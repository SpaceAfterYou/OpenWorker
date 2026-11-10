using System.Numerics;
using OpenWorker.Batch.Actions;
using OpenWorker.Gameplay.Ai;

namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// What a creature of the table cannot be thrown by: bit 1 turns the throws that lift it into flinches, bit 2 turns the push
/// back on its feet into one, both leave it where it stands.
/// </summary>
[Flags]
public enum ReactionLimit : byte
{
    None = 0,
    NoLaunch = 1,
    NoKnockBack = 2,
    Immovable = NoLaunch | NoKnockBack
}

/// <summary>
/// A blow as it reaches a target: what the attack event says it does, and where it comes from.
/// </summary>
/// <param name="Reaction">The throw the event names.</param>
/// <param name="Kind">The kind of reaction the event names.</param>
/// <param name="DamageRate">The share of the damage the event deals; an event that deals none throws nobody.</param>
/// <param name="Arc">How wide the area of the attack is, in degrees. A monster that strikes all round pushes outward.</param>
/// <param name="Origin">The place the blow comes from: the attacker for a blow of its body, the shot or the trap otherwise.</param>
/// <param name="AttackerPosition">Where the attacker stands.</param>
/// <param name="Yaw">The way the attacker looks, in degrees.</param>
/// <param name="ByMonster">The attacker is a monster.</param>
/// <param name="AttackerWeight">The weight rank of the attacker; a player has none.</param>
/// <param name="Charging">How much a charged skill adds to the throw (0.5 is half as far again).</param>
/// <param name="IsMelee">The blow is struck by the body of the attacker; a shot, a trap and a chain strike where they are.</param>
public readonly record struct Blow(
    AttackReaction Reaction,
    ReactionKind Kind,
    float DamageRate,
    float Arc,
    Vector3 Origin,
    Vector3 AttackerPosition,
    float Yaw,
    bool ByMonster,
    int AttackerWeight = 0,
    float Charging = 0f,
    bool IsMelee = true);

/// <summary>
/// The target of a blow, as far as the throw cares.
/// </summary>
/// <param name="Position">Where the target stands.</param>
/// <param name="Weight">The weight rank of the target.</param>
/// <param name="Limit">What the target cannot be thrown by.</param>
/// <param name="StandType">How the target stands: 2 and 3 are fixed in their place.</param>
/// <param name="Defense">The defense type of the target when the blow lands.</param>
/// <param name="IsAirborne">The target is in the air.</param>
/// <param name="IsHeld">The attacker holds the target, which then goes where the attacker goes.</param>
public readonly record struct Struck(
    Vector3 Position,
    int Weight,
    ReactionLimit Limit,
    int StandType,
    DefenseType Defense,
    bool IsAirborne,
    bool IsHeld = false);

/// <summary>
/// The two dice a throw rolls: how much farther or nearer it goes, and how far its way is turned.
/// </summary>
/// <param name="Distance">Between -20 and 20 units.</param>
/// <param name="Angle">Between -15 and 15, in half degrees.</param>
public readonly record struct BlowRolls(int Distance, int Angle)
{
    public static BlowRolls Roll(Random random) => new(random.Next(41) - 20, random.Next(31) - 15);
}

/// <summary>
/// Where a throw takes its target.
/// </summary>
/// <param name="Vector">
/// What the clients are told: for a launch or a knock down, the way from the target in the frame of the attacker (forward,
/// away, up) which the client turns by the yaw of the blow; for any other reaction, the world offset of the push.
/// </param>
/// <param name="Yaw">The way the target flies, in degrees: where the attacker looks, with the turn of the event.</param>
public readonly record struct ExtraMoveResult(Vector3 Vector, float Yaw);

/// <summary>
/// How far, how high and which way a blow throws what it hits. One rule for the blows of players and of monsters.
/// </summary>
public static class ExtraMove
{
    /// <summary>
    /// A target that weighs this many ranks less than the attacker, or fewer, flies farther.
    /// </summary>
    private const int HeavyGap = 2;

    private const float HeavyScale = 1.35f;
    private const float LightScale = 0.65f;
    private const int LightGap = -2;
    private const int LightestGap = -3;
    private const int NoPushGap = -4;
    private const float GuardScale = 0.5f;
    private const float RollTurn = 0.5f;
    private const float HalfTurn = 180f;
    private const float QuarterTurn = 90f;
    private const float FullTurn = 360f;

    /// <summary>
    /// The throw of one blow on one target.
    /// </summary>
    public static ExtraMoveResult Of(in Blow blow, in Struck target, in BlowRolls rolls)
    {
        var yaw = blow.Yaw;
        var vector = Vector3.Zero;
        var push = blow.Reaction;

        if (target.Limit != ReactionLimit.Immovable && !target.IsHeld)
        {
            yaw += push.Angle;

            if (blow.ByMonster && blow.Arc > HalfTurn)
            {
                yaw = AiMath.YawTo(blow.AttackerPosition, target.Position);
            }

            if (blow.DamageRate > 0f)
            {
                vector = Scale(Start(blow, target, rolls), blow, target);

                if (blow.Kind is not (ReactionKind.LaunchUp or ReactionKind.KnockDownStraight or ReactionKind.KnockDown or ReactionKind.Stun))
                {
                    vector = Turn(vector, blow, target, yaw, rolls);
                }
            }
        }

        // A blow that moves nobody leaves nobody moved, whatever the dice say.
        if (!push.Moves)
        {
            vector = Vector3.Zero;
        }

        // A blow that comes from a shot pushes away from where the shot burst.
        if (!blow.IsMelee && push.Arrow == AttackReaction.AwayFromOrigin)
        {
            yaw = AiMath.YawTo(blow.Origin, target.Position);
        }

        return new ExtraMoveResult(vector, yaw);
    }

    private static Vector3 Start(in Blow blow, in Struck target, in BlowRolls rolls)
    {
        var push = blow.Reaction;
        var forward = push.Arrow == AttackReaction.StraightAlongAttacker ? -push.Distance : -(push.Distance + rolls.Distance);
        var up = target.IsAirborne ? push.HeightInAir : push.Height;

        return new Vector3(0f, blow.Kind == ReactionKind.Stun ? 0f : forward, up);
    }

    /// <summary>
    /// A heavy attacker throws a light target farther, a light one a heavy target not at all; a guard takes half, a charge
    /// adds. Only the throws that lift the target count its weight.
    /// </summary>
    private static Vector3 Scale(Vector3 vector, in Blow blow, in Struck target)
    {
        var lifts = blow.Kind is ReactionKind.LaunchUp or ReactionKind.KnockDown or ReactionKind.KnockDownStraight;

        if (lifts && (blow.Reaction.UsesTargetWeight || target.StandType >= 2))
        {
            var gap = blow.AttackerWeight - target.Weight;

            if (gap >= HeavyGap)
            {
                vector *= HeavyScale;
            }
            else if (gap is LightGap or LightestGap)
            {
                vector *= LightScale;
            }
            else if (gap <= NoPushGap)
            {
                vector = Vector3.Zero;
            }
        }

        if (target.Defense == DefenseType.Guard)
        {
            vector *= GuardScale;
        }

        return blow.Charging > 0f ? vector * (blow.Charging + 1f) : vector;
    }

    /// <summary>
    /// A push that keeps the target on its feet goes in the world: turned by the way the attacker looks, a little at random,
    /// or away from the place the blow came from.
    /// </summary>
    private static Vector3 Turn(Vector3 vector, in Blow blow, in Struck target, float yaw, in BlowRolls rolls)
    {
        var turn = blow.Reaction.Arrow switch
        {
            AttackReaction.AlongAttacker => yaw + (rolls.Angle * RollTurn),
            AttackReaction.AwayFromOrigin => AwayFrom(blow.Origin, target.Position),
            AttackReaction.StraightAlongAttacker => yaw,
            _ => 0f
        };

        var flat = AiMath.Rotate(new Vector2(vector.X, vector.Y), turn);

        return new Vector3(flat.X, flat.Y, vector.Z);
    }

    /// <summary>
    /// The angle that turns a push forward into a push from the origin through the target.
    /// </summary>
    private static float AwayFrom(Vector3 origin, Vector3 target)
    {
        var toward = MathF.Atan2(origin.Y - target.Y, origin.X - target.X) * (HalfTurn / MathF.PI);
        var angle = toward - QuarterTurn;

        return angle < 0f ? angle + FullTurn : angle;
    }
}
