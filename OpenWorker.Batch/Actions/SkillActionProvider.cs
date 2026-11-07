using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Enums;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Batch.Actions;

/// <summary>
/// One attack of a skill: when it lands after the skill starts, and which attack of its animation it is.
/// </summary>
public readonly record struct SkillHit(float Time, short TriggerIndex, AttackTrigger Trigger);

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

            // The index counts every attack of the animation, because that is how the client finds it.
            hits.AddRange(animation.Attacks
                .Select((attack, index) => new SkillHit(offset + attack.Start, (short)index, attack))
                .Where(e => e.Trigger.HitsArea));

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
