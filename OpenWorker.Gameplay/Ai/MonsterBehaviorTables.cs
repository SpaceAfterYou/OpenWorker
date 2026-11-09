using System.Collections.ObjectModel;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Ai;

/// <summary>
/// What a monster does, from its row in the table.
/// </summary>
/// <param name="ActionFile">The name of the file of its animations and attacks.</param>
/// <param name="AiScript">The script that says how it thinks, or nothing.</param>
/// <param name="Skills">The skills it can use, ten slots; a slot without a skill is zero.</param>
/// <param name="Rank">How strong it is, from the plain monsters (0) up to the bosses.</param>
/// <param name="Faction">The side it is on: who its enemies are depends on it.</param>
public sealed record MonsterBehavior(
    int Id,
    string ActionFile,
    string AiScript,
    float WalkSpeed,
    float RunSpeed,
    float TurnSpeed,
    float GazeSpeed,
    IReadOnlyList<int> Skills,
    byte StandType = 0,
    byte Rank = 0,
    byte Faction = 0)
{
    /// <summary>
    /// Monsters of this rank and above do not go looking for the helpers of the players.
    /// </summary>
    public const byte IgnoresFollowersFromRank = 3;

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

    /// <summary>
    /// How two sides stand to each other, by the side that looks and the side it looks at.
    /// </summary>
    private Dictionary<(byte Local, byte Other), byte> Relations { get; }

    /// <summary>
    /// The relation of two sides that do not fight each other.
    /// </summary>
    private const byte Friendly = 2;

    public MonsterBehaviorTables(ReadOnlyCollection<MonsterRow> monsters, ReadOnlyCollection<SkillRow> skills, ReadOnlyCollection<FactionRow>? factions = null)
    {
        Relations = (factions ?? new ReadOnlyCollection<FactionRow>([]))
            .DistinctBy(e => (e.LocalFaction, e.OtherFaction))
            .ToDictionary(e => (e.LocalFaction, e.OtherFaction), e => e.Relation);

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

    /// <summary>
    /// Whether a monster of one side fights one of the other side. Sides the table does not pair fight each other.
    /// </summary>
    public bool AreEnemies(byte side, byte other)
    {
        return !Relations.TryGetValue((side, other), out var relation) || relation != Friendly;
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
            (byte)row.StandType,
            row.Rank,
            row.Faction);
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
            row.TargetType != 0);
    }
}
