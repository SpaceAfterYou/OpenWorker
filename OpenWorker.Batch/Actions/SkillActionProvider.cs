using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Enums;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Batch.Actions;

/// <summary>
/// One attack of a skill and when it lands after the skill starts.
/// </summary>
public readonly record struct SkillHit(float Time, AttackTrigger Trigger);

/// <summary>
/// Finds what a skill hits from the animations the table names for it.
/// </summary>
public sealed class SkillActionProvider(ReadOnlyCollection<SkillRow> skills, ILogger<SkillActionProvider> logger)
{
    private const string Extension = ".adf";
    private const string HeroFilePrefix = "PC_";

    /// <summary>
    /// The table says this when a skill has no animation for a step.
    /// </summary>
    private const string NoAnimation = "0";

    /// <summary>
    /// Skills that depend on the direction of movement have one animation for each. Without input a character goes forward.
    /// </summary>
    private const string ForwardSuffix = "_F";

    private const byte FlownByClient = 2;

    private const byte Charging = 2;
    private const byte ChargingToTheEnd = 5;
    private const byte ChargingBothSides = 8;

    /// <summary>
    /// The trap this skill places follows the cursor of the player until the skill is over.
    /// </summary>
    private const byte FollowsCursor = 4;

    /// <summary>
    /// Where the action files of the client are.
    /// </summary>
    public string Folder { get; init; } = "action";

    private Dictionary<int, SkillRow> Rows { get; } = skills.DistinctBy(e => e.Id).ToDictionary(e => e.Id);
    private Dictionary<string, ActionFile?> Files { get; } = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<(int Skill, string File), SkillHit[]> Hits { get; } = [];
    private Dictionary<(int Skill, string File), SkillHit[]> Shots { get; } = [];

    public IReadOnlyList<SkillHit> GetHits(int skill, Hero hero)
    {
        return GetHits(skill, GetHeroFileName(hero));
    }

    /// <summary>
    /// The attacks of a skill in an action file, by the name of the file without the extension. Monsters have a file each.
    /// </summary>
    public IReadOnlyList<SkillHit> GetHits(int skill, string actionFile)
    {
        if (Hits.TryGetValue((skill, actionFile), out var hits))
        {
            return hits;
        }

        hits = Rows.TryGetValue(skill, out var row) && GetFile(actionFile) is { } file ? Collect(row, file) : [];

        Hits.Add((skill, actionFile), hits);

        return hits;
    }

    /// <summary>
    /// The moments an animation of a skill sets something flying: the arrows, bullets and blasts that a monster fires.
    /// </summary>
    public IReadOnlyList<SkillHit> GetShots(int skill, string actionFile)
    {
        if (Shots.TryGetValue((skill, actionFile), out var shots))
        {
            return shots;
        }

        shots = Rows.TryGetValue(skill, out var row) && GetFile(actionFile) is { } file ? Collect(row, file, attack => attack.IsProjectile) : [];

        Shots.Add((skill, actionFile), shots);

        return shots;
    }

    /// <summary>
    /// How long the animations of a skill last together, in seconds. Zero when the file has none of them.
    /// </summary>
    public float GetDuration(int skill, string actionFile)
    {
        if (!Rows.TryGetValue(skill, out var row) || GetFile(actionFile) is not { } file)
        {
            return 0f;
        }

        var total = 0f;

        foreach (var name in (ReadOnlySpan<string>)[row.AnimationStart, row.AnimationLoop, row.AnimationEnd])
        {
            if (Find(file, name) is { } animation)
            {
                total += animation.Length;
            }
        }

        return total;
    }

    /// <summary>
    /// The number the clients know an animation of a file by, or zero when the file does not have it.
    /// </summary>
    public int GetAnimationIndex(string actionFile, string animation)
    {
        return GetFile(actionFile) is { } file && file.Indexes.TryGetValue(animation, out var index) ? index : 0;
    }

    /// <summary>
    /// How long an animation lasts, in seconds. Zero when the file does not have it.
    /// </summary>
    public float GetAnimationLength(string actionFile, string animation)
    {
        return GetFile(actionFile) is { } file && Find(file, animation) is { } found ? found.Length : 0f;
    }

    /// <summary>
    /// What a skill plays, step by step, and how its charge grows. Null when nothing is known about the skill.
    /// </summary>
    public SkillAnimations? GetAnimations(int skill, Hero hero)
    {
        if (!Rows.TryGetValue(skill, out var row) || GetFile(GetHeroFileName(hero)) is not { } file)
        {
            return null;
        }

        var charging = row.ControlType is Charging or ChargingToTheEnd;
        var steps = charging ? ChargeSteps(row) : [];

        return new SkillAnimations(
            Find(file, row.AnimationStart),
            Find(file, row.AnimationLoop),
            Find(file, row.AnimationExtra),
            Find(file, row.AnimationEnd),
            row.LoopTime / 1000f,
            charging || row.ControlType == ChargingBothSides,
            steps,
            Enumerable.Range(1, steps.Count).Select(level => Find(file, $"{row.AnimationEnd}_{level:00}", false)).ToArray());
    }

    private static List<float> ChargeSteps(SkillRow row)
    {
        return new[] { row.ChargeTime1, row.ChargeTime2, row.ChargeTime3, row.ChargeTime4 }
            .Take(row.ChargeCount)
            .Select(e => e / 1000f)
            .ToList();
    }

    /// <summary>
    /// Finds the attack a client says it has just fired.
    /// </summary>
    public bool TryGetProjectile(int skill, Hero hero, int attack, out AttackTrigger trigger)
    {
        return TryGetProjectile(skill, GetHeroFileName(hero), attack, out trigger);
    }

    public bool TryGetProjectile(int skill, string actionFile, int attack, out AttackTrigger trigger)
    {
        return TryGet(skill, actionFile, attack, e => e.IsProjectile, out trigger);
    }

    /// <summary>
    /// Finds the chain a client says it has just let go.
    /// </summary>
    public bool TryGetChain(int skill, Hero hero, int attack, out AttackTrigger trigger)
    {
        return TryGet(skill, GetHeroFileName(hero), attack, e => e.IsChain && e.Chain is not null && e.Projectile is not null, out trigger);
    }

    /// <summary>
    /// Whether the player moves the trap of this skill with the mouse.
    /// </summary>
    public bool FollowsMouse(int skill)
    {
        return Rows.TryGetValue(skill, out var row) && row.ControlType == FollowsCursor;
    }

    /// <summary>
    /// How far the skill reaches, as the table says.
    /// </summary>
    public float GetMaxRange(int skill)
    {
        return Rows.TryGetValue(skill, out var row) ? row.RangeMax : 0f;
    }

    private bool TryGet(int skill, string actionFile, int attack, Func<AttackTrigger, bool> accepts, out AttackTrigger trigger)
    {
        trigger = default;

        if (!Rows.TryGetValue(skill, out var row) || GetFile(actionFile) is not { } file)
        {
            return false;
        }

        foreach (var name in (ReadOnlySpan<string>)[row.AnimationStart, row.AnimationLoop, row.AnimationExtra, row.AnimationEnd])
        {
            foreach (var candidate in Find(file, name)?.Attacks ?? [])
            {
                if (candidate.Id == attack && accepts(candidate))
                {
                    trigger = candidate;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Some skills fly on the client alone, which then expects no word about it.
    /// </summary>
    public bool IsFlownByClient(int skill)
    {
        return Rows.TryGetValue(skill, out var row) && row.BulletSyncType == FlownByClient;
    }

    private static SkillHit[] Collect(SkillRow row, ActionFile file)
    {
        return Collect(row, file, attack => attack.HitsArea || attack.IsTrap);
    }

    private static SkillHit[] Collect(SkillRow row, ActionFile file, Func<AttackTrigger, bool> wanted)
    {
        var hits = new List<SkillHit>();
        var offset = 0f;

        // The extra animation plays only on input from the player.
        foreach (var name in (ReadOnlySpan<string>)[row.AnimationStart, row.AnimationLoop, row.AnimationEnd])
        {
            if (Find(file, name) is not { } animation)
            {
                continue;
            }

            hits.AddRange(animation.Attacks
                .Where(wanted)
                .Select(attack => new SkillHit(offset + attack.Start, attack)));

            offset += animation.Length;
        }

        return hits.ToArray();
    }

    private static ActionAnimation? Find(ActionFile file, string name, bool forward = true)
    {
        if (string.IsNullOrEmpty(name) || name == NoAnimation)
        {
            return null;
        }

        return file.Animations.GetValueOrDefault(name) ?? (forward ? file.Animations.GetValueOrDefault(name + ForwardSuffix) : null);
    }

    private static string GetHeroFileName(Hero hero)
    {
        return $"{HeroFilePrefix}{(char)('A' + (int)hero - 1)}";
    }

    private ActionFile? GetFile(string name)
    {
        if (Files.TryGetValue(name, out var file))
        {
            return file;
        }

        var path = Path.Join(Folder, name + Extension);

        try
        {
            file = ActionFile.Load(path);
        }

        catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException or EndOfStreamException)
        {
            logger.LogWarning(e, "Skills of {Name} will not hit anything: cannot read {Path}", name, path);
        }

        Files.Add(name, file);

        return file;
    }
}
