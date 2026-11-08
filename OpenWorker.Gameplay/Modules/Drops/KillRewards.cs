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
        var given = experience.GiveForMonster(player, monster);

        LogKilled(logger, monster, given);

        results.AddKill(player, given);
        drops.DropForKill(player, monster, position);
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
