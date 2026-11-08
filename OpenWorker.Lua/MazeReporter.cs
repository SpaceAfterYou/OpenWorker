using Arch.Core;
using Microsoft.Extensions.Logging;
using OpenWorker.Gameplay.Modules.DailyMissions;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Gameplay.Modules.SoulMetry;
using OpenWorker.Gameplay.Modules.Vaccum;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Lua;

/// <summary>
/// What the maze tells the rest of the game: who was killed, which sector and maze were cleared, which object was
/// used, and which of its objects belong to soul metries and cubes. Missions, quests and soul metries hear it here. A failure on the other side never reaches the fight.
/// </summary>
public sealed partial class MazeReporter(
    DailyMissionManager missions,
    SoulMetryManager soulMetries,
    SoulMetryCatalog soulCatalog,
    VaccumManager cubes,
    QuestManager quests,
    ILogger<MazeReporter> logger)
{
    // The kinds of attack the table calls plain, from the ground, thrown and skills proper.
    private const byte PlainAttack = 1;
    private const byte ThrownWeapon = 3;
    private const byte AttackFromGround = 7;
    private const byte SkillWithChoice = 18;

    /// <summary>
    /// The kinds of attack that killed a monster, as the daily missions ask for them: one bit for each of
    /// <see cref="DailyMissionCondition"/>. The attack is known by the kind of its skill.
    /// </summary>
    public static uint GetKillConditions(byte passiveType, bool critical)
    {
        var conditions = passiveType switch
        {
            PlainAttack => Bit(DailyMissionCondition.NormalAttack),
            AttackFromGround => Bit(DailyMissionCondition.RiseAttack),
            ThrownWeapon => Bit(DailyMissionCondition.SubweaponAttack),
            0 or SkillWithChoice => Bit(DailyMissionCondition.SkillAttack),
            _ => 0u
        };

        return critical ? conditions | Bit(DailyMissionCondition.Critical) : conditions;
    }

    private static uint Bit(DailyMissionCondition condition) => 1u << (int)condition;

    /// <summary>
    /// The person has killed a monster.
    /// </summary>
    public async ValueTask MonsterKilledAsync(Entity player, int monster, uint conditions)
    {
        try
        {
            await missions.OnMonsterKilledAsync(player, monster, conditions, CancellationToken.None);
            await quests.OnMonsterKilledAsync(player, monster, CancellationToken.None);
        }

        catch (Exception e)
        {
            LogReportFailed(logger, e, "MonsterKilled");
        }
    }

    /// <summary>
    /// The person has won a maze. Nothing ranks a run yet, so a run has no rank.
    /// </summary>
    public async ValueTask MazeClearedAsync(Entity player, MazeInfoRow info, int seconds, int hero, byte level)
    {
        try
        {
            var clear = new DailyMissionMazeClear(info.Id, 0, seconds, info.Type, level, hero, []);

            await missions.OnMazeClearedAsync(player, clear, false, CancellationToken.None);
            await soulMetries.OnMazeClearedAsync(player, info.Group, CancellationToken.None);
            await quests.OnMazeClearedAsync(player, info.Id, CancellationToken.None);
        }

        catch (Exception e)
        {
            LogReportFailed(logger, e, "MazeCleared");
        }
    }

    /// <summary>
    /// The person has cleared a sector of a maze.
    /// </summary>
    public async ValueTask SectorClearedAsync(Entity player, int maze, int sector)
    {
        try
        {
            await quests.OnSectorClearedAsync(player, maze, sector, CancellationToken.None);
        }

        catch (Exception e)
        {
            LogReportFailed(logger, e, "SectorCleared");
        }
    }

    /// <summary>
    /// The person has used an object of a maze by its box.
    /// </summary>
    public async ValueTask ObjectUsedAsync(Entity player, int box)
    {
        try
        {
            await quests.OnObjectUsedAsync(player, box, CancellationToken.None);
        }

        catch (Exception e)
        {
            LogReportFailed(logger, e, "ObjectUsed");
        }
    }

    /// <summary>
    /// The interaction objects that soul metries put in a maze.
    /// </summary>
    public IReadOnlySet<int> GetSoulObjects(int maze)
    {
        return soulCatalog.All
            .SelectMany(e => e.Objects)
            .Where(e => e.Maze == maze)
            .Select(e => e.ObjectId)
            .ToHashSet();
    }

    /// <summary>
    /// The objects of soul metries that show up for the person this time.
    /// </summary>
    public IReadOnlySet<int> GetSoulObjectsToShow(Entity player, int maze)
    {
        return soulCatalog.All
            .Where(e => e.Objects.Any(o => o.Maze == maze))
            .SelectMany(e => soulMetries.GetObjectsToSpawn(player, e.Id, maze))
            .ToHashSet();
    }

    /// <summary>
    /// The person clicks an object of a maze. If it belongs to a soul metry, the soul metry finds it.
    /// </summary>
    public async ValueTask CollectSoulObjectAsync(Entity player, int maze, int interaction)
    {
        try
        {
            var found = soulCatalog.All.FirstOrDefault(e => e.Objects.Any(o => o.Maze == maze && o.ObjectId == interaction));

            if (found is not null)
            {
                await soulMetries.CollectAsync(player, found.Id, interaction, CancellationToken.None);
            }
        }

        catch (Exception e)
        {
            LogReportFailed(logger, e, "CollectSoulObject");
        }
    }

    /// <summary>
    /// A cube lies in the maze for the person to take. The value tells which reward it holds.
    /// </summary>
    public void RegisterCube(Entity player, int box, int interaction, TimeSpan duration)
    {
        cubes.Register(player, box, interaction, duration);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "The report {Report} of the maze failed; the fight goes on")]
    private static partial void LogReportFailed(ILogger logger, Exception exception, string report);
}
