namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// How a blow throws its target, as the event of the attack names it. The numbers are the ones the clients read.
/// </summary>
public enum ReactionKind : byte
{
    None = 0,

    /// <summary>The target flinches and may slide a little.</summary>
    Flinch = 1,

    /// <summary>The target is thrown up and falls on its back.</summary>
    LaunchUp = 2,

    /// <summary>The target is knocked off its feet and flies away backwards.</summary>
    KnockDown = 3,

    /// <summary>The target is knocked flat in a straight line.</summary>
    KnockDownStraight = 4,

    /// <summary>The target is pushed back on its feet.</summary>
    KnockBack = 5,

    /// <summary>The target is stunned where it stands.</summary>
    Stun = 6
}

/// <summary>
/// What a creature is doing, as the clients name it: a motion class. Only the classes a blow puts a creature into are named.
/// </summary>
public static class MotionClass
{
    public const short None = -1;
    public const short Idle = 1;
    public const short FlinchLeft = 15;
    public const short FlinchRight = 16;
    public const short KnockBack = 17;

    /// <summary>Knocked down in a straight line.</summary>
    public const short DownStraight = 18;

    /// <summary>Thrown up.</summary>
    public const short DownUp = 19;

    /// <summary>Knocked down forward.</summary>
    public const short DownForward = 20;

    /// <summary>Knocked down backward.</summary>
    public const short DownBackward = 21;

    public const short Stunned = 22;
    public const short HitInAir = 23;
    public const short ArmorBreakStart = 24;
    public const short ArmorBreakLoop = 25;
    public const short ArmorBreakEnd = 26;

    /// <summary>
    /// Whether the motion is one of the four ways of being knocked down.
    /// </summary>
    public static bool IsKnockedDown(short motion) => motion is >= DownStraight and <= DownBackward;

    /// <summary>
    /// Whether the motion is a flinch or a push back on the feet.
    /// </summary>
    public static bool IsFlinch(short motion) => motion is >= FlinchLeft and <= KnockBack;
}
