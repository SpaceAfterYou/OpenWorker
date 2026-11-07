using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenWorker.Gameplay.Modules.DailyMissions;
using OpenWorker.Gameplay.Modules.Persons;
using OpenWorker.Hotspot;

namespace OpenWorker.Gameplay.Modules.Drops;

/// <summary>
/// What a player gets when a monster dies in its maze: experience, and things on the ground.
/// </summary>
public sealed class KillRewards(ExperienceService experience, DropService drops, DailyMissionManager? missions, MazeResults results, ILogger<KillRewards> logger)
{
    public void OnKilled(Entity player, int monster, Vector3 position)
    {
        logger.LogDebug("Monster {Monster} killed, worth {Experience} experience", monster, experience.ForMonster(monster));

        results.AddKill(player, experience.GiveForMonster(player, monster));
        drops.DropForKill(player, monster, position);

        if (missions is not null)
        {
            _ = ReportAsync(player, monster);
        }
    }

    // The missions are saved, which takes a while, and the kill does not wait for it.
    private async Task ReportAsync(Entity player, int monster)
    {
        try
        {
            await missions!.OnMonsterKilledAsync(player, monster, 0, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Counting the kill of monster {Monster} for the missions failed", monster);
        }
    }
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
public sealed class DropExpiryService(WorldLoop loop, DropService drops, ILogger<DropExpiryService> logger) : BackgroundService
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
                logger.LogError(exception, "Clearing the ground failed");
            }
        }
    }
}
