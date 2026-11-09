using System.Collections.ObjectModel;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Ai;

/// <summary>
/// What a monster does, from its row in the table.
/// </summary>
/// <param name="ActionFile">The name of the file of its animations and attacks.</param>
/// <param name="AiScript">The script that says how it thinks, or nothing.</param>
/// <param name="Skills">The skills it can use, ten slots; a slot without a skill is zero.</param>
public sealed record MonsterBehavior(
    int Id,
    string ActionFile,
    string AiScript,
    float WalkSpeed,
    float RunSpeed,
    float TurnSpeed,
    float GazeSpeed,
    IReadOnlyList<int> Skills,
    byte StandType = 0)
{
    /// <summary>
    /// Fixed in its place for good: it can turn and attack but never walks.
    /// </summary>
    public bool IsRooted => StandType is 2 or 3;

    /// <summary>
    /// Stands still until it has been drawn into a fight.
    /// </summary>
    public bool WalksOnlyInBattle => StandType == 1;
}

/// <summary>
/// Finds the rows that decide how a monster behaves.
/// </summary>
public sealed class MonsterBehaviorTables
{
    private Dictionary<int, MonsterBehavior> Monsters { get; }
    private Dictionary<int, AiSkill> Skills { get; }
    private Dictionary<int, float> AnimationTimes { get; }

    public MonsterBehaviorTables(ReadOnlyCollection<MonsterRow> monsters, ReadOnlyCollection<SkillRow> skills)
    {
        Monsters = monsters
            .DistinctBy(e => e.Id)
            .ToDictionary(e => e.Id, Create);

        Skills = skills
            .DistinctBy(e => e.Id)
            .ToDictionary(e => e.Id, Create);

        AnimationTimes = skills
            .DistinctBy(e => e.Id)
            .ToDictionary(e => e.Id, e => e.Duration * 0.001f);
    }

    public MonsterBehavior? Get(int monster)
    {
        return Monsters.GetValueOrDefault(monster);
    }

    public AiSkill? GetSkill(int skill)
    {
        return Skills.GetValueOrDefault(skill);
    }

    /// <summary>
    /// How long the table says a skill lasts, in seconds.
    /// </summary>
    public float GetSkillDuration(int skill)
    {
        return AnimationTimes.GetValueOrDefault(skill);
    }

    private static MonsterBehavior Create(MonsterRow row)
    {
        return new MonsterBehavior(
            row.Id,
            row.ActionFile,
            row.AiScript,
            row.WalkSpeed,
            row.RunSpeed,
            row.TurnSpeed,
            row.GazeSpeed,
            [row.Skill1, row.Skill2, row.Skill3, row.Skill4, row.Skill5, row.Skill6, row.Skill7, row.Skill8, row.Skill9, row.Skill10],
            (byte)row.StandType);
    }

    private static AiSkill Create(SkillRow row)
    {
        return new AiSkill(
            row.Id,
            row.RangeMin,
            row.RangeMax,
            row.Cooldown * 0.001f,
            row.CooldownGroup,
            row.GlobalCooldown * 0.001f,
            row.Angle,
            row.RotationType != 0,
            false);
    }
}
