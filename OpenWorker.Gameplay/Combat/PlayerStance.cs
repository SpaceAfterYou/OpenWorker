namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// A player thrown by a blow the server dealt: in the air until it lands, then on the ground until it gets up.
/// </summary>
/// <param name="Lands">The game time the flight is over at.</param>
/// <param name="GetsUp">The game time the player begins to get up at, unless its client gets it up sooner.</param>
public readonly record struct PlayerPostureComponent(TimeSpan Lands, TimeSpan GetsUp);

/// <summary>
/// Whether a player is in the air or lies on the ground. Only a throw the server dealt puts it there: the client gets its
/// character up by itself and says so only when a key gets it up sooner, so what it tells may end the lying, never begin it.
/// </summary>
public static class PlayerStance
{
    private const float Gravity = 1960f;
    private const float HighestLift = 200f;
    private const float HangTime = 0.3f;
    private const float GroundDelay = 0.2f;
    private const float AirDelay = 0.05f;

    /// <summary>
    /// The step of a knock-down motion in which the player gets up.
    /// </summary>
    public const short Rising = 5;

    /// <summary>
    /// How long a player that has landed plays its fall before it lies: the end animation of a knock-down, the same for every
    /// hero.
    /// </summary>
    public static TimeSpan Landing { get; } = TimeSpan.FromSeconds(0.533);

    /// <summary>
    /// How long a player lies before it gets up by itself.
    /// </summary>
    public static TimeSpan GroundDown { get; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// A throw to a height: up and down again, with the moment it hangs at the top and, for a knock-down, the moment before it
    /// leaves the ground; then the fall and the time on the ground.
    /// </summary>
    /// <param name="inTheAir">Whether a throw already holds the player in the air.</param>
    public static PlayerPostureComponent Thrown(TimeSpan now, ReactionKind kind, float height, bool inTheAir)
    {
        var lift = Math.Clamp(height, 0f, HighestLift);
        var delay = kind == ReactionKind.KnockDown ? inTheAir ? AirDelay : GroundDelay : 0f;
        var flight = delay + 2f * MathF.Sqrt(2f * lift / Gravity) + HangTime;
        var lands = now + TimeSpan.FromSeconds(flight);

        return new PlayerPostureComponent(lands, lands + Landing + GroundDown);
    }

    public static bool IsInTheAir(in PlayerPostureComponent posture, TimeSpan now) => now < posture.Lands;

    public static bool IsLying(in PlayerPostureComponent posture, TimeSpan now) => now >= posture.Lands && now < posture.GetsUp;

    /// <summary>
    /// What the motion the client says its player plays leaves of the throw: a knock-down motion short of getting up only
    /// confirms it; getting up, or any other motion, ends it.
    /// </summary>
    public static PlayerPostureComponent Told(in PlayerPostureComponent posture, short motion, short step)
    {
        return MotionClass.IsKnockedDown(motion) && step != Rising ? posture : default;
    }

    /// <summary>
    /// The player starts a skill: whatever it lay in is over, and a flight goes on.
    /// </summary>
    public static PlayerPostureComponent Acts(in PlayerPostureComponent posture, TimeSpan now)
    {
        return IsLying(posture, now) ? posture with { GetsUp = now } : posture;
    }
}
