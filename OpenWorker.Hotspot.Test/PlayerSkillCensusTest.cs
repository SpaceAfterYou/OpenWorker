using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Enums;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The census of the skills of every hero in the client's tables and action files: every attack of every animation the client
/// plays for a skill of a player lands, or is let go as a shot, by some requests the client sends (06/08 starts the skill, 06/41
/// holds and lets go the key, 06/4A follows it with the animation of a combo), for the level and divergence it is for. A new way
/// of the client to play an animation that the server does not follow fails here.
/// </summary>
[TestClass]
public sealed class PlayerSkillCensusTest
{
    private const string ActionsVariable = "OPENWORKER_TEST_ACTION";

    private const byte Charging = 2;
    private const byte ChargingToTheEnd = 5;
    private const byte ChargingBothSides = 8;
    private const byte Directions = 1;

    /// <summary>
    /// The divergences a skill may be played with: none, and the levels of the two groups the events of this client name.
    /// </summary>
    private static readonly int[] Divergences =
    [
        0, ActionFilter.FirstGroup + 1, ActionFilter.FirstGroup + 2, ActionFilter.FirstGroup + 3, ActionFilter.SecondGroup + 1,
        ActionFilter.SecondGroup + 2, ActionFilter.SecondGroup + 3
    ];

    private const float Tick = 0.02f;
    private const float Longest = 15f;

    [TestMethod]
    public void EveryAttackOfEveryPlayerSkillLandsBySomeRequest()
    {
        var skills = TestTables.Load<SkillRow>("tb_Skill");
        var folder = Environment.GetEnvironmentVariable(ActionsVariable);

        if (string.IsNullOrEmpty(folder))
        {
            Assert.Inconclusive($"{ActionsVariable} is not set.");
        }

        var provider = new SkillActionProvider(skills, NullLogger<SkillActionProvider>.Instance) { Folder = folder };
        var missed = new List<string>();
        var attacks = 0;

        foreach (var hero in Enum.GetValues<Hero>().Where(e => (int)e is >= 1 and <= 6))
        {
            var file = ActionFile.Load(Path.Join(folder, $"PC_{(char)('A' + (int)hero - 1)}.adf"));
            var wanted = new Dictionary<int, string>();
            var landed = new HashSet<int>();

            foreach (var row in skills.Where(e => e.UseClass == (byte)hero)
                         .DistinctBy(e => (e.AnimationStart, e.AnimationLoop, e.AnimationExtra, e.AnimationEnd, e.ControlType, e.ChargeCount, e.Level, e.DivergenceGroup1 != 0)))
            {
                if (provider.GetAnimations(row.Id, hero) is not { } animations)
                {
                    continue;
                }

                var played = ClientPlays(row, file);
                var divergences = row.DivergenceGroup1 != 0 || row.DivergenceGroup2 != 0 ? Divergences : [0];

                foreach (var clip in played)
                {
                    foreach (var attack in clip.Attacks.Where(e => e.HitsArea || e.IsTrap || e.IsProjectile || e.IsChain))
                    {
                        if (divergences.Any(d => !new SkillMode(row.Level, d).At(ActionFilter.Any).Excludes(attack.Filter)))
                        {
                            wanted.TryAdd(attack.Id, $"{hero} {row.Id} {clip.Name} attack {attack.Id}");
                        }
                    }
                }

                var combos = played.Where(e => played.Any(c => (c.Combos ?? []).Any(o => o.Left.Concat(o.Right).Any(t => string.Equals(t.Animation, e.Name, StringComparison.OrdinalIgnoreCase))))).ToList();

                foreach (var divergence in divergences)
                {
                    var mode = new SkillMode(row.Level, divergence);

                    foreach (var hold in Holds(animations))
                    {
                        landed.UnionWith(Play(animations, mode, hold, null));

                        foreach (var combo in combos)
                        {
                            landed.UnionWith(Play(animations, mode, hold, combo));
                        }
                    }
                }
            }

            attacks += wanted.Count;
            missed.AddRange(wanted.Where(e => !landed.Contains(e.Key)).Select(e => e.Value));
        }

        Assert.IsGreaterThan(1000, attacks, "The census counts the attacks of every hero.");
        Assert.IsEmpty(missed, $"{missed.Count} of {attacks} attacks land by no request:\n{string.Join("\n", missed.Take(40))}");
    }

    /// <summary>
    /// The animations the client plays for the skill: the ones of the table (with the four directions of a skill that has them),
    /// the ends of the levels of a charge, and those the combos of all of them lead to.
    /// </summary>
    private static List<ActionAnimation> ClientPlays(SkillRow row, ActionFile file)
    {
        var names = new List<string>();

        foreach (var name in (string[])[row.AnimationStart, row.AnimationLoop, row.AnimationExtra, row.AnimationEnd])
        {
            if (name is "" or "0")
            {
                continue;
            }

            names.Add(name);

            if (row.Direction == Directions)
            {
                names.AddRange(new[] { "_F", "_L", "_R", "_B" }.Select(e => name + e));
            }
        }

        if (row.ControlType is Charging or ChargingToTheEnd)
        {
            names.AddRange(Enumerable.Range(1, row.ChargeCount).Select(e => string.Create(CultureInfo.InvariantCulture, $"{row.AnimationEnd}_{e:00}")));
        }

        if (row.ControlType == ChargingBothSides)
        {
            names.Add(row.AnimationEnd + "_Attack01");
            names.Add(row.AnimationEnd + "_Attack02");
        }

        var played = names.Select(e => file.Animations.GetValueOrDefault(e)).OfType<ActionAnimation>().Distinct().ToList();

        for (var i = 0; i < played.Count; i++)
        {
            foreach (var step in (played[i].Combos ?? []).SelectMany(e => e.Left.Concat(e.Right)))
            {
                if (file.Animations.GetValueOrDefault(step.Animation) is { } next && !played.Contains(next))
                {
                    played.Add(next);
                }
            }
        }

        return played;
    }

    /// <summary>
    /// How long the key is held from the start: not at all, as long as the skill lets it, let go as soon as a charge begins, and to
    /// each level of a charge.
    /// </summary>
    private static IEnumerable<float> Holds(SkillAnimations animations)
    {
        yield return 0f;
        yield return Longest;

        var held = Tick;

        yield return held + (animations.Start?.Length ?? 0f);

        foreach (var step in animations.ChargeSteps)
        {
            held += step;

            yield return held + (animations.Start?.Length ?? 0f) + Tick;
        }
    }

    /// <summary>
    /// Plays the skill holding the key from the start for the given time, asking for the animation of a combo again and again until
    /// it is taken. Returns the attacks that landed and the shots that were let go.
    /// </summary>
    private static HashSet<int> Play(SkillAnimations animations, SkillMode mode, float hold, ActionAnimation? combo)
    {
        var playback = new SkillPlayback(animations, 1f, mode);
        var landed = new HashSet<int>();
        var pressed = false;

        for (var now = 0f; now < Longest + 5f && !playback.IsDone; now += Tick)
        {
            landed.UnionWith(playback.Advance(now).Select(e => e.Trigger.Id));

            if (playback.Playing is { } playing)
            {
                foreach (var shot in playing.Clip.Attacks.Where(e => e.IsProjectile || e.IsChain))
                {
                    while (playback.TakeShot(shot.Id, now))
                    {
                        landed.Add(shot.Id);
                    }
                }
            }

            if (hold > 0f && !pressed && now < hold)
            {
                pressed = true;
                playback.Press(true, now);
            }
            else if (pressed && now >= hold)
            {
                pressed = false;
                playback.Press(false, now);
            }

            if (combo is not null && playback.Combo(combo, now))
            {
                combo = null;
            }
        }

        return landed;
    }
}
