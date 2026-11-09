using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Hosting;
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
    /// A monster of a maze died by the hand of <paramref name="killer"/>. The experience goes to <paramref name="present"/>,
    /// the members of the party in the maze who are on their feet (<see cref="ExperienceService.GiveForParty"/>), and the
    /// killer alone when nobody is; what falls on the ground is the killer's.
    /// </summary>
    public void OnKilled(Entity killer, IReadOnlyList<Entity> present, int monster, Vector3 position)
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
        drops.DropForKill(killer, monster, position);
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

/// <summary>
/// Takes away what has lain on the ground too long.
/// </summary>
public sealed partial class DropExpiryService(WorldLoop loop, DropService drops, ILogger<DropExpiryService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await loop.InvokeAsync(drops.Expire);
            }
            catch (Exception exception)
            {
                LogExpiryFailed(logger, exception);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Clearing the ground failed")]
    private static partial void LogExpiryFailed(ILogger logger, Exception exception);
}
