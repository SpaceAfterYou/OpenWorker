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
    private const string Folder = "action";
    private const string Extension = ".adf";
    private const string HeroFilePrefix = "PC_";

    /// <summary>
    /// Skills that depend on the direction of movement have one animation for each. Without input a character goes forward.
    /// </summary>
    private const string ForwardSuffix = "_F";

    private const byte FlownByClient = 2;

    private Dictionary<int, SkillRow> Rows { get; } = skills.DistinctBy(e => e.Id).ToDictionary(e => e.Id);
    private Dictionary<string, ActionFile?> Files { get; } = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<(int Skill, string File), SkillHit[]> Hits { get; } = [];

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
    /// Finds the attack a client says it has just fired.
    /// </summary>
    public bool TryGetProjectile(int skill, Hero hero, int attack, out AttackTrigger trigger)
    {
        return TryGetProjectile(skill, GetHeroFileName(hero), attack, out trigger);
    }

    public bool TryGetProjectile(int skill, string actionFile, int attack, out AttackTrigger trigger)
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
                if (candidate.Id == attack && candidate.IsProjectile)
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
                .Where(attack => attack.HitsArea || attack.IsTrap)
                .Select(attack => new SkillHit(offset + attack.Start, attack)));

            offset += animation.Length;
        }

        return hits.ToArray();
    }

    private static ActionAnimation? Find(ActionFile file, string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        return file.Animations.GetValueOrDefault(name) ?? file.Animations.GetValueOrDefault(name + ForwardSuffix);
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
