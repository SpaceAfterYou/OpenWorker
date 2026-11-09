using Arch.Core;

namespace OpenWorker.Gameplay.Modules.Helpers;

/// <summary>
/// What a person and its maze look like when the person calls a helper.
/// </summary>
/// <param name="SinceLastSummon">How long ago the person last called a helper.</param>
/// <param name="MazeTakesHelpers">The maze allows helpers at all.</param>
/// <param name="Owned">The person has the helper.</param>
/// <param name="Summoned">The helper stands in the maze already.</param>
/// <param name="HelpersInMaze">How many helpers stand in the maze, of every player in it.</param>
/// <param name="PartyMembers">How many people the party of the person has, or one for a person alone.</param>
public readonly record struct HelperSummonCase(
    bool OwnerAlive,
    TimeSpan SinceLastSummon,
    bool InMaze,
    bool MazeTakesHelpers,
    bool Owned,
    bool Summoned,
    int HelpersInMaze,
    int PartyMembers);

/// <summary>
/// The rules of calling a helper into a maze and sending it away, checked in the order the game checks them, each with the
/// code the client shows.
/// </summary>
public static class HelperRules
{
    /// <summary>
    /// A person calls one helper at most every ten seconds.
    /// </summary>
    public static readonly TimeSpan SummonDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// A maze holds four bodies at most: the people of the party and every helper in it.
    /// </summary>
    public const int MaxBodies = 4;

    /// <summary>
    /// "Cooldown": a helper was called less than ten seconds ago.
    /// </summary>
    public const ushort Cooldown = 58005;

    /// <summary>
    /// "Cannot summon more": the maze holds as many bodies as it can.
    /// </summary>
    public const ushort Full = 58008;

    /// <summary>
    /// The call goes ahead.
    /// </summary>
    public const ushort Allowed = 0;

    /// <summary>
    /// Whether one more helper fits: the helpers in the maze and the people of the party (one alone) stay under four.
    /// </summary>
    public static bool HasRoom(int helpersInMaze, int partyMembers)
    {
        return helpersInMaze + Math.Max(1, partyMembers) < MaxBodies;
    }

    /// <summary>
    /// The answer to a call: nothing at all for a person who is down, a refusal code, or <see cref="Allowed"/>.
    /// </summary>
    public static ushort? CheckSummon(in HelperSummonCase call)
    {
        if (!call.OwnerAlive)
        {
            return null;
        }

        if (call.SinceLastSummon < SummonDelay)
        {
            return Cooldown;
        }

        if (!call.InMaze)
        {
            return HelperManager.NotInMaze;
        }

        if (!call.MazeTakesHelpers || !call.Owned || call.Summoned)
        {
            return HelperManager.CannotSummon;
        }

        return HasRoom(call.HelpersInMaze, call.PartyMembers) ? Allowed : Full;
    }

    /// <summary>
    /// The answer to sending a helper away: nothing for a person who is down, a refusal code, or <see cref="Allowed"/>.
    /// </summary>
    public static ushort? CheckRelease(bool ownerAlive, bool inMaze, bool summoned)
    {
        if (!ownerAlive)
        {
            return null;
        }

        if (!inMaze)
        {
            return HelperManager.NotInMaze;
        }

        return summoned ? Allowed : HelperManager.CannotSummon;
    }
}

/// <summary>
/// The place helpers are called to: the maze. A server without one answers every call as a district does.
/// </summary>
public interface IHelperField
{
    /// <summary>
    /// The person calls a helper into its maze.
    /// </summary>
    ValueTask SummonAsync(Entity player, int helper, CancellationToken cancellationToken);

    /// <summary>
    /// The person sends one of its helpers away.
    /// </summary>
    void Release(Entity player, int helper);

    /// <summary>
    /// The person calls its helpers back to where it stands.
    /// </summary>
    void Recall(Entity player);
}
