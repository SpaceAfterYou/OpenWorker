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
    private Dictionary<Hero, ActionFile?> Files { get; } = [];
    private Dictionary<(int Skill, Hero Hero), SkillHit[]> Hits { get; } = [];

    public IReadOnlyList<SkillHit> GetHits(int skill, Hero hero)
    {
        if (Hits.TryGetValue((skill, hero), out var hits))
        {
            return hits;
        }

        hits = Rows.TryGetValue(skill, out var row) && GetFile(hero) is { } file ? Collect(row, file) : [];

        Hits.Add((skill, hero), hits);

        return hits;
    }

    /// <summary>
    /// Finds the attack a client says it has just fired.
    /// </summary>
    public bool TryGetProjectile(int skill, Hero hero, int attack, out AttackTrigger trigger)
    {
        trigger = default;

        if (!Rows.TryGetValue(skill, out var row) || GetFile(hero) is not { } file)
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

    private ActionFile? GetFile(Hero hero)
    {
        if (Files.TryGetValue(hero, out var file))
        {
            return file;
        }

        var path = Path.Join(Folder, $"{HeroFilePrefix}{(char)('A' + (int)hero - 1)}{Extension}");

        try
        {
            file = ActionFile.Load(path);
        }

        catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            logger.LogWarning(e, "Skills of {Hero} will not hit anything: cannot read {Path}", hero, path);
        }

        Files.Add(hero, file);

        return file;
    }
}
