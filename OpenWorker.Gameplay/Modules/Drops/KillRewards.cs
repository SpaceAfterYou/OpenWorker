using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Logging;
using OpenWorker.Gameplay.Modules.Persons;
using OpenWorker.Hotspot;

namespace OpenWorker.Gameplay.Modules.Drops;

/// <summary>
/// What a player gets when a monster dies in its maze: experience, and things on the ground. The missions and quests
/// hear of the kill from the maze itself, which knows how it was done.
/// </summary>
public sealed partial class KillRewards(ExperienceService experience, DropService drops, MazeResults results, ILogger<KillRewards> logger)
{
    public void OnKilled(Entity player, int monster, Vector3 position)
    {
        OnKilled(player, [player], monster, position);
    }

    /// <summary>
    /// As <see cref="OnKilled(Entity, IReadOnlyList{Entity}, IReadOnlyList{Entity}, int, Vector3)"/> for a maze whose players
    /// who stand to share the experience are all there are.
    /// </summary>
    public void OnKilled(Entity killer, IReadOnlyList<Entity> present, int monster, Vector3 position)
    {
        OnKilled(killer, present, present, monster, position);
    }

    /// <summary>
    /// A monster of a maze died by the hand of <paramref name="killer"/>. The experience goes to <paramref name="present"/>,
    /// the members of the party in the maze who are on their feet (<see cref="ExperienceService.GiveForParty"/>), and the
    /// killer alone when nobody is. What falls on the ground falls for each of <paramref name="everyone"/> in the maze, on
    /// their feet or not, a drop of their own.
    /// </summary>
    public void OnKilled(Entity killer, IReadOnlyList<Entity> present, IReadOnlyList<Entity> everyone, int monster, Vector3 position)
    {
        IReadOnlyList<Entity> members = present.Count == 0 ? [killer] : present;
        var given = experience.GiveForParty(members, monster);
        var killerGot = 0;

        for (var index = 0; index < members.Count; index++)
        {
            if (members[index] == killer)
            {
                killerGot = given[index];
            }
            else
            {
                results.AddExperience(members[index], given[index]);
            }
        }

        LogKilled(logger, monster, killerGot);

        results.AddKill(killer, killerGot);
        drops.DropForKill(everyone.Count == 0 ? [killer] : everyone, monster, position);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Monster {Monster} killed, {Experience} experience given")]
    private static partial void LogKilled(ILogger logger, int monster, int experience);
}

/// <summary>
/// Forgets what lies on the ground for a player who has left.
/// </summary>
public sealed class DropCleaner(DropService drops) : ISessionCloseHandler
{
    public ValueTask OnSessionClosedAsync(Entity player)
    {
        drops.Forget(player);

        return ValueTask.CompletedTask;
    }
}
