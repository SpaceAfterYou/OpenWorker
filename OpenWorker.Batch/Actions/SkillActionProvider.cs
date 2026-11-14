using System.Collections.ObjectModel;
using System.Globalization;
using System.Numerics;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Enums;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Batch.Actions;

/// <summary>
/// One attack of a skill and when it lands after the skill starts.
/// </summary>
public readonly record struct SkillHit(float Time, AttackTrigger Trigger);

/// <summary>
/// A moment of a skill at which the monster that plays it hides.
/// </summary>
public readonly record struct SkillHiding(float Time, InvisibleEvent Event);

/// <summary>
/// A moment of a skill at which the monster that plays it lets go of a player it holds.
/// </summary>
/// <param name="Time">From the start of the skill.</param>
/// <param name="DropTime">How long the player flies to the place it is put down at.</param>
/// <param name="FollowUp">The blow that lands on the player there, if the file names one found.</param>
public readonly record struct SkillDetach(float Time, float DropTime, AttackTrigger? FollowUp);

/// <summary>
/// A move the animation of a skill carries its mover by: the moment, from the start of the skill, the move in the mover's own
/// frame (forward is -Y), and the animation it belongs to, counted in the order the skill plays them.
/// </summary>
public readonly record struct SkillMove(float Time, Vector3 Offset, int Clip);

/// <summary>
/// The parts of a grabbing skill.
/// </summary>
public enum GrabStage
{
    Start,

    /// <summary>
    /// What plays after the start while the monster holds a player.
    /// </summary>
    Holding,

    /// <summary>
    /// What plays after the start when the monster has taken nobody.
    /// </summary>
    Missed
}

/// <summary>
/// One animation of a skill with everything that happens in it, times from the start of the animation.
/// </summary>
public sealed record SkillStage(float Length, IReadOnlyList<SkillHit> Hits, IReadOnlyList<SkillHit> Shots, IReadOnlyList<SkillHiding> Hidings, IReadOnlyList<SkillDetach> Detaches, ActionAnimation? Clip = null)
{
    public static SkillStage Empty { get; } = new(0f, [], [], [], []);

    /// <summary>
    /// How the animation carries the monster, in the order of time.
    /// </summary>
    public IReadOnlyList<SkillMove> Moves { get; init; } = [];
}

/// <summary>
/// An animation of a skill on the clock: when it begins, from the start of the skill, and how long it lasts, in seconds.
/// </summary>
public readonly record struct TimedClip(float Began, float Length, ActionAnimation Animation);

/// <summary>
/// A skill as a monster plays it, on the clock: its animations, its blows, its shots and its hidings at the moments the clients
/// play them, and how long it lasts. Times are in seconds from the start of the skill.
/// </summary>
public sealed record SkillTimeline(float Length, IReadOnlyList<TimedClip> Clips, IReadOnlyList<SkillHit> Hits, IReadOnlyList<SkillHit> Shots, IReadOnlyList<SkillHiding> Hidings)
{
    public static SkillTimeline Empty { get; } = new(0f, [], [], [], []);

    /// <summary>
    /// How the animations carry the monster, on the same clock, in the order of time. <see cref="SkillMove.Clip"/> is the place of
    /// the animation in <see cref="Clips"/>.
    /// </summary>
    public IReadOnlyList<SkillMove> Moves { get; init; } = [];

    /// <summary>
    /// The moments the monster marks where its target stands, for the traps that go there, in the order of time.
    /// </summary>
    public IReadOnlyList<float> TrapMarks { get; init; } = [];
}

/// <summary>
/// A moment of a skill at which the character that plays it starts to carry away what stands before it.
/// </summary>
public readonly record struct SkillCarry(float Time, AttachEvent Event);

/// <summary>
/// A moment of a skill at which the character that plays it takes control of a monster or lets it go, from the start of the skill.
/// </summary>
/// <param name="Animation">The number of the animation the monster comes in with in the character's file, or -1 when it has none.</param>
public readonly record struct SkillControl(float Time, ControlEvent Event, int Animation);

/// <summary>
/// Finds what a skill hits from the animations the table names for it.
/// </summary>
public sealed partial class SkillActionProvider(ReadOnlyCollection<SkillRow> skills, ILogger<SkillActionProvider> logger)
{
    private const string Extension = ".adf";
    private const string HeroFilePrefix = "PC_";

    /// <summary>
    /// The file of a hero that says what its akashic skill does with a called card.
    /// </summary>
    private const string AkashicSuffix = "_Akashic";

    /// <summary>
    /// The table says this when a skill has no animation for a step.
    /// </summary>
    private const string NoAnimation = "0";

    /// <summary>
    /// Skills that depend on the direction of movement have one animation for each. Without input a character goes forward.
    /// </summary>
    private const string ForwardSuffix = "_F";

    private const byte FlownByClient = 2;
    private const byte GrabSkillType = 8;

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
    private Dictionary<(int Skill, string File), SkillTimeline> Timelines { get; } = [];

    /// <summary>
    /// How a skill plays out for a monster, which presses no key: the start, the loop for the time of the table, then the end, each
    /// stretch of an animation at the speed its file gives it. Empty when the file has none of the animations.
    /// </summary>
    public SkillTimeline GetTimeline(int skill, string actionFile)
    {
        if (Timelines.TryGetValue((skill, actionFile), out var timeline))
        {
            return timeline;
        }

        timeline = Rows.TryGetValue(skill, out var row) && GetFile(actionFile) is { } file ? Lay(row, file) : SkillTimeline.Empty;

        Timelines.Add((skill, actionFile), timeline);

        return timeline;
    }

    private static SkillTimeline Lay(SkillRow row, ActionFile file)
    {
        var clips = new List<TimedClip>();
        var hits = new List<SkillHit>();
        var shots = new List<SkillHit>();
        var hidings = new List<SkillHiding>();
        var moves = new List<SkillMove>();
        var marks = new List<float>();
        var offset = 0f;

        // The extra animation plays only on input from the player.
        foreach (var (name, loop) in (ReadOnlySpan<(string, bool)>)[(row.AnimationStart, false), (row.AnimationLoop, true), (row.AnimationEnd, false)])
        {
            if (Find(file, name) is not { } animation)
            {
                continue;
            }

            var clock = new SkillPlayback.ClipClock(animation, 1f);

            // A loop that starts over by itself lasts the time of the table and judges its attacks again each time round.
            var rounds = loop && animation.Repeats && row.LoopTime > 0 && clock.Length > 0f;
            var end = offset + (rounds ? row.LoopTime / 1000f : clock.Length);
            var round = offset;

            do
            {
                clips.Add(new TimedClip(round, Math.Min(clock.Length, end - round), animation));

                // A round after the first goes on from the end of the one before, and takes the move of the frame at its start.
                foreach (var frame in Frames(animation, round > offset))
                {
                    var time = round + clock.Wall(frame.Time);

                    if (!rounds || time < end)
                    {
                        moves.Add(new SkillMove(time, frame.Offset, clips.Count - 1));
                    }
                }

                foreach (var attack in animation.Attacks)
                {
                    var time = round + clock.Wall(attack.Start);

                    if (rounds && time >= end)
                    {
                        continue;
                    }

                    if (attack.IsProjectile)
                    {
                        shots.Add(new SkillHit(time, attack));
                    }
                    else if (attack.HitsArea || attack.IsTrap)
                    {
                        hits.Add(new SkillHit(time, attack));
                    }
                }

                hidings.AddRange((animation.Hides ?? []).Select(hide => new SkillHiding(round + clock.Wall(hide.Start), hide)).Where(e => !rounds || e.Time < end));
                marks.AddRange((animation.TrapMarks ?? []).Select(mark => round + clock.Wall(mark)).Where(e => !rounds || e < end));

                round += clock.Length;
            }
            while (rounds && round < end);

            offset = end;
        }

        return new SkillTimeline(
            offset,
            clips,
            hits.OrderBy(e => e.Time).ToArray(),
            shots.OrderBy(e => e.Time).ToArray(),
            hidings.OrderBy(e => e.Time).ToArray())
        {
            Moves = moves,
            TrapMarks = [.. marks.Order()]
        };
    }

    /// <summary>
    /// The frames of the way an animation carries its mover as it plays once from its start: a frame counts once its moment is
    /// passed, so the one at the very start counts only when the animation goes on from the end of a round before.
    /// </summary>
    private static IEnumerable<MoveFrame> Frames(ActionAnimation animation, bool goesOn)
    {
        return (animation.Moves ?? []).Where(e => (goesOn ? e.Time >= 0f : e.Time > 0f) && e.Time <= animation.Length);
    }

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
    /// The moments at which a skill of a hero starts to carry away what stands before the hero, from the start of the skill.
    /// </summary>
    public IReadOnlyList<SkillCarry> GetCarries(int skill, Hero hero)
    {
        if (!Rows.TryGetValue(skill, out var row) || GetFile(GetHeroFileName(hero)) is not { } file)
        {
            return [];
        }

        var carries = new List<SkillCarry>();
        var offset = 0f;

        foreach (var name in (ReadOnlySpan<string>)[row.AnimationStart, row.AnimationLoop, row.AnimationEnd])
        {
            if (Find(file, name) is not { } animation)
            {
                continue;
            }

            carries.AddRange((animation.Attaches ?? []).Select(carry => new SkillCarry(offset + carry.Start, carry)));

            offset += animation.Length;
        }

        return carries;
    }

    /// <summary>
    /// The moments at which a skill of a hero takes control of a monster or lets it go, from the start of the skill. A loop with a
    /// time of its own lasts that time, whether the file has its animation or not: the clients play it all the same.
    /// </summary>
    public IReadOnlyList<SkillControl> GetControls(int skill, Hero hero)
    {
        if (!Rows.TryGetValue(skill, out var row) || GetFile(GetHeroFileName(hero)) is not { } file)
        {
            return [];
        }

        var controls = new List<SkillControl>();
        var offset = 0f;

        foreach (var name in (ReadOnlySpan<string>)[row.AnimationStart, row.AnimationLoop, row.AnimationEnd])
        {
            var animation = Find(file, name);

            controls.AddRange((animation?.Controls ?? []).Select(control =>
                new SkillControl(offset + control.Start, control, file.Indexes.TryGetValue(control.Animation, out var index) ? index : -1)));

            offset += name == row.AnimationLoop && name != NoAnimation && row.LoopTime > 0 ? row.LoopTime / 1000f : animation?.Length ?? 0f;
        }

        return controls;
    }

    /// <summary>
    /// Whether the skill grabs: it plays its start, then the animation of holding the target if the monster holds one, or the
    /// one of missing it if not, and ends.
    /// </summary>
    public bool IsGrabSkill(int skill)
    {
        return Rows.TryGetValue(skill, out var row) && row.SkillType == GrabSkillType;
    }

    /// <summary>
    /// What one animation of a grabbing skill holds, with its times counted from the start of that animation. Empty when the
    /// file does not have the animation.
    /// </summary>
    public SkillStage GetStage(int skill, string actionFile, GrabStage stage)
    {
        if (!Rows.TryGetValue(skill, out var row) || GetFile(actionFile) is not { } file)
        {
            return SkillStage.Empty;
        }

        var name = stage switch
        {
            GrabStage.Start => row.AnimationStart,
            GrabStage.Holding => row.AnimationEnd,
            _ => row.AnimationExtra
        };

        if (Find(file, name) is not { } animation)
        {
            return SkillStage.Empty;
        }

        var detaches = new List<SkillDetach>();

        foreach (var detach in animation.Detaches ?? [])
        {
            var following = Find(file, detach.Animation)?.Attacks;
            AttackTrigger? followUp = following is not null && detach.Event >= 0 && detach.Event < following.Count ? following[detach.Event] : null;

            detaches.Add(new SkillDetach(detach.Start, detach.DropTime, followUp));
        }

        return new SkillStage(
            animation.Length,
            animation.Attacks.Where(e => e.HitsArea || e.IsTrap).Select(e => new SkillHit(e.Start, e)).ToArray(),
            animation.Attacks.Where(e => e.IsProjectile).Select(e => new SkillHit(e.Start, e)).ToArray(),
            (animation.Hides ?? []).Select(e => new SkillHiding(e.Start, e)).ToArray(),
            detaches,
            animation)
        {
            Moves = Frames(animation, false).Select(e => new SkillMove(e.Time, e.Offset, 0)).ToArray()
        };
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
    /// Whether an animation starts over by itself until something stops it, as standing does.
    /// </summary>
    public bool IsLooping(string actionFile, string animation)
    {
        return GetFile(actionFile) is { } file && Find(file, animation) is { Repeats: true };
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
            Enumerable.Range(1, steps.Count).Select(level => Find(file, ChargedEnd(row, level), false)).ToArray());
    }

    private static List<float> ChargeSteps(SkillRow row)
    {
        return new[] { row.ChargeTime1, row.ChargeTime2, row.ChargeTime3, row.ChargeTime4 }
            .Take(row.ChargeCount)
            .Select(e => e / 1000f)
            .ToList();
    }

    /// <summary>
    /// What the akashic skill of a hero does when a card of the given group is called: the events of the akashic file under
    /// the card's group and the animation the skill plays, which take the place of the animation's own. Every hero uses the
    /// file of the first hero, as the original does (the files of the others are empty). Empty when nothing is known.
    /// </summary>
    public IReadOnlyList<AkashicSummon> GetAkashicSummons(int skill, int group)
    {
        if (!Rows.TryGetValue(skill, out var row) || GetSummons() is not { } summons)
        {
            return [];
        }

        foreach (var name in (ReadOnlySpan<string>)[row.AnimationStart, row.AnimationLoop, row.AnimationEnd])
        {
            if (!string.IsNullOrEmpty(name) && name != NoAnimation &&
                summons.TryGetValue(string.Create(CultureInfo.InvariantCulture, $"{group}:{name}"), out var found))
            {
                return found;
            }
        }

        return [];
    }

    private IReadOnlyDictionary<string, IReadOnlyList<AkashicSummon>>? _summons;
    private bool _summonsRead;

    private IReadOnlyDictionary<string, IReadOnlyList<AkashicSummon>>? GetSummons()
    {
        if (_summonsRead)
        {
            return _summons;
        }

        _summonsRead = true;

        var path = Path.Join(Folder, HeroFilePrefix + "A" + AkashicSuffix + Extension);

        try
        {
            using var stream = new MemoryStream(File.ReadAllBytes(path));

            _summons = ActionFile.ReadSummons(stream);
        }

        catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException or EndOfStreamException)
        {
            LogUnreadable(logger, e, "the akashic skill", path);
        }

        return _summons;
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

    /// <summary>
    /// What kind of attack the table says a skill is: 1 for the plain attacks, 7 for the ones from the ground, 3 for
    /// a thrown weapon, 0 and 18 for the skills proper. Zero when the table does not know the skill.
    /// </summary>
    public byte GetPassiveType(int skill)
    {
        return Rows.TryGetValue(skill, out var row) ? row.PassiveType : (byte)0;
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
            if (Find(file, name) is { } animation && TryFind(animation, attack, accepts, out trigger))
            {
                return true;
            }
        }

        // A charged skill ends with the animation of the level its charge reached, which has shots of its own.
        if (row.ControlType is Charging or ChargingToTheEnd)
        {
            for (var level = 1; level <= ChargeSteps(row).Count; level++)
            {
                if (Find(file, ChargedEnd(row, level), false) is { } animation && TryFind(animation, attack, accepts, out trigger))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryFind(ActionAnimation animation, int attack, Func<AttackTrigger, bool> accepts, out AttackTrigger trigger)
    {
        foreach (var candidate in animation.Attacks)
        {
            if (candidate.Id == attack && accepts(candidate))
            {
                trigger = candidate;
                return true;
            }
        }

        trigger = default;

        return false;
    }

    private static string ChargedEnd(SkillRow row, int level)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{row.AnimationEnd}_{level:00}");
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
            LogUnreadable(logger, e, name, path);
        }

        Files.Add(name, file);

        return file;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skills of {Name} will not hit anything: cannot read {Path}")]
    private static partial void LogUnreadable(ILogger logger, Exception exception, string name, string path);
}
