namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// A player thrown by a blow the server dealt: in the air until it lands, then on the ground until it gets up.
/// </summary>
/// <param name="Lands">The game time the flight is over at.</param>
/// <param name="GetsUp">The game time the player begins to get up at, unless its client gets it up sooner.</param>
/// <param name="Rises">The game time the player starts to rise at.</param>
/// <param name="From">How high above the ground the player rises from.</param>
/// <param name="Lift">How much higher it rises before it hangs and falls.</param>
public readonly record struct PlayerPostureComponent(TimeSpan Lands, TimeSpan GetsUp, TimeSpan Rises = default, float From = 0f, float Lift = 0f);

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
    /// A launch that catches the player in the air lifts it at least this much more, whatever height it has gained.
    /// </summary>
    private const float LeastLiftInTheAir = 50f;

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
    /// leaves the ground; then the fall and the time on the ground. A launch that catches the player in the air lifts it by what
    /// the blow asks less the height it has already gained, never less than a little and never above the highest a throw goes;
    /// a knock-down puts it on the ground first and throws it from there, as high as it could still have gone.
    /// </summary>
    /// <param name="current">What the throws before it left: the player is in the air when one still holds it there.</param>
    public static PlayerPostureComponent Thrown(TimeSpan now, ReactionKind kind, float height, in PlayerPostureComponent current)
    {
        var gained = IsInTheAir(current, now) ? HeightAt(current, now) : 0f;
        var asked = Math.Max(height, 0f);
        var lift = gained > 0f && kind != ReactionKind.KnockDown ? Math.Max(asked - gained, LeastLiftInTheAir) : asked;

        lift = Math.Clamp(lift, 0f, Math.Max(HighestLift - gained, 0f));

        var from = kind == ReactionKind.KnockDown ? 0f : gained;
        var delay = kind == ReactionKind.KnockDown ? IsInTheAir(current, now) ? AirDelay : GroundDelay : 0f;
        var rises = now + TimeSpan.FromSeconds(delay);
        var lands = rises + TimeSpan.FromSeconds(RiseTime(lift) + HangTime + FallTime(from + lift));

        return new PlayerPostureComponent(lands, lands + Landing + GroundDown, rises, from, lift);
    }

    /// <summary>
    /// How high above the ground a throw holds the player at a moment of its flight.
    /// </summary>
    public static float HeightAt(in PlayerPostureComponent posture, TimeSpan now)
    {
        if (!IsInTheAir(posture, now))
        {
            return 0f;
        }

        var time = (float)(now - posture.Rises).TotalSeconds;

        if (time <= 0f)
        {
            return posture.From;
        }

        var rise = RiseTime(posture.Lift);

        if (time < rise)
        {
            return posture.From + MathF.Sqrt(2f * posture.Lift * Gravity) * time - 0.5f * Gravity * time * time;
        }

        var fall = Math.Max(time - rise - HangTime, 0f);

        return Math.Max(posture.From + posture.Lift - 0.5f * Gravity * fall * fall, 0f);
    }

    private static float RiseTime(float lift) => MathF.Sqrt(2f * lift / Gravity);

    private static float FallTime(float height) => MathF.Sqrt(2f * height / Gravity);

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
