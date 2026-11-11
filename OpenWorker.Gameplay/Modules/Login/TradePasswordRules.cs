using OpenWorker.Hotspot.Messages.Response.Person.Enums;

namespace OpenWorker.Gameplay.Modules.Login;

/// <summary>
/// What the wrong tries at the trade password of an account lead to. The count belongs to the account and is kept in its
/// row, so it outlasts a login; a right password sets it back to zero, and so does making the password again.
/// </summary>
public static class TradePasswordRules
{
    /// <summary>
    /// The password locks at this many wrong tries (and at every multiple of it): nothing is accepted until the password is
    /// reset by a game master.
    /// </summary>
    public const int LockedAt = 10;

    /// <summary>
    /// After this many wrong tries (and after every multiple of it) the password is not tried for a while.
    /// </summary>
    public const int PausedAt = 3;

    /// <summary>
    /// How long the password is not tried after the third wrong try, and after each third after it. The tries made in that
    /// time are not counted.
    /// </summary>
    public static readonly TimeSpan Pause = TimeSpan.FromMinutes(3);

    /// <summary>
    /// Whether the count of wrong tries has locked the password.
    /// </summary>
    public static bool IsLocked(int failed) => failed > 0 && failed % LockedAt == 0;

    /// <summary>
    /// Whether the password is not tried now: the last wrong try was the third (or the sixth, ninth...) and it was less than
    /// <see cref="Pause"/> ago.
    /// </summary>
    public static bool IsPaused(int failed, DateTime? lastFailedAt, DateTime now)
    {
        return failed > 0 && failed % PausedAt == 0 && lastFailedAt is { } last && last > now - Pause;
    }

    /// <summary>
    /// Where a player stands with the password when it enters, before it has given it: no password, a locked one, or one
    /// to give.
    /// </summary>
    public static PasswordProtectionState StateAtEntry(bool hasPassword, int failed)
    {
        if (!hasPassword)
        {
            return PasswordProtectionState.None;
        }

        return IsLocked(failed) ? PasswordProtectionState.Lock : PasswordProtectionState.UnAuthenticated;
    }
}
