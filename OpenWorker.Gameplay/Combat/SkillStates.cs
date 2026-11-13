namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// A player who has been thrown into the air by a blow, until the moment the server knows it lands. The server only knows the
/// flight: it does not follow the player on the ground, where the client lets it get up when it likes.
/// </summary>
/// <param name="DownUntil">The game time the flight is over at.</param>
public readonly record struct PlayerPostureComponent(TimeSpan DownUntil);

/// <summary>
/// What the state of a player says about the skills it may start, as the original decides it. A skill names the state it is for:
/// 1 is for any state, 3 for a player on the ground, 4 for one in a counter-attack hit, 2 and 5 for a motion or a buff; any
/// other is for a player who stands, and for one in a hit only if its normal state says so.
/// </summary>
public static class SkillStates
{
    /// <summary>
    /// The skill is not for the state the player is in (the answer 56009 is for the player that cannot attack at all).
    /// </summary>
    public const ushort NotForThisState = 56016;

    private const byte AllowsDown = 4;
    private const byte NeedsBuff = 0x10;
    private const float Gravity = 1960f;

    /// <summary>
    /// Whether a player that is thrown into the air may start a skill. Only the skills of the plain kind are refused: the ones
    /// for other states ask for something the server does not know.
    /// </summary>
    public static bool CanStartInTheAir(byte useState, byte normalState)
    {
        return useState != 0 || (normalState & AllowsDown) != 0;
    }

    /// <summary>
    /// The answer to a skill that is not for the state: 56009 for the kinds that ask for a motion, a counter hit or a buff, and
    /// 56016 for the plain one.
    /// </summary>
    public static ushort Refusal(byte useState, byte normalState)
    {
        return (useState & 5) != 0 || (normalState & NeedsBuff) != 0 ? SkillRefusal.CannotAttack : NotForThisState;
    }

    /// <summary>
    /// How long a throw to a height keeps the player in the air at least: up and down again, in milliseconds.
    /// </summary>
    public static long FlightMilliseconds(float height)
    {
        return height > 0f ? (long)(2000f * MathF.Sqrt(2f * height / Gravity)) : 0L;
    }
}
