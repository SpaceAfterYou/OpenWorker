namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// The motion the client of a player last said it plays, and the step of it: how a blow left the player.
/// </summary>
public readonly record struct PlayerStanceComponent(short Motion, short Step);

/// <summary>
/// Whether a player lies on the ground, as its client tells it. A blow of a monster reaches a player that lies only when its
/// event says so, and a monster starts a skill on one only when the skill is for it.
/// </summary>
public static class PlayerStance
{
    /// <summary>
    /// The motion of a player that has fallen dead on the ground.
    /// </summary>
    public const short DownDead = 13;

    private const short Landed = 1;
    private const short HitWhileLying = 3;

    /// <summary>
    /// How long after its flight a player may still lie, at most: the landing, three seconds on the ground and a margin. The
    /// client is believed only that long after a throw the server dealt.
    /// </summary>
    public static TimeSpan MostLying { get; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Whether the motion and its step are those of a player on the ground: knocked down and landed, lying or hit while lying.
    /// </summary>
    public static bool IsDown(PlayerStanceComponent stance)
    {
        return stance.Motion == DownDead || MotionClass.IsKnockedDown(stance.Motion) && stance.Step is >= Landed and <= HitWhileLying;
    }
}
