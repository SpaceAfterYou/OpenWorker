namespace OpenWorker.Batch.Actions;

/// <summary>
/// The parts a skill plays: the start, then the loop or the wait, then the end. A clip may be missing.
/// </summary>
/// <param name="LoopTime">How long the loop lasts, in seconds. Zero means the loop plays as long as the key is held.</param>
/// <param name="StartsPressed">Skills that charge count the key as held from the start.</param>
/// <param name="ChargeSteps">When the charge grows a level, in seconds from the start of the loop. Empty for skills that do not charge.</param>
/// <param name="ChargedEnds">The end of the skill for each level of charge. A level without one plays <paramref name="End"/>.</param>
public sealed record SkillAnimations(
    ActionAnimation? Start,
    ActionAnimation? Loop,
    ActionAnimation? Extra,
    ActionAnimation? End,
    float LoopTime,
    bool StartsPressed,
    IReadOnlyList<float> ChargeSteps,
    IReadOnlyList<ActionAnimation?> ChargedEnds);

public enum SkillStep
{
    Start,
    Loop,
    Extra,
    End,
    Done
}

/// <summary>
/// How a skill plays out in time, step by step, and what is judged on the way. The key the player holds decides
/// what comes after the start: a skill with a wait plays its loop only when the key is held, a loop goes on as long
/// as the key is held (or for the time the table says), and letting go of the key ends it.
/// The animations play at the attack speed of the player, and a stretch of an animation that has a speed of its own plays at
/// that speed, as on the client; a blow that lands holds the animation for a moment (<see cref="Pause"/>).
/// All times count from the moment the skill began, in seconds.
/// </summary>
public sealed class SkillPlayback
{
    /// <summary>
    /// A loop that has no time of its own and a key that is never let go would last forever.
    /// </summary>
    public const float LongestHold = 10f;

    /// <summary>
    /// The wait of a skill ends by itself after this long.
    /// </summary>
    public const float WaitTime = 3f;

    /// <summary>
    /// How far the client may play a skill ahead of the server or behind it, in seconds: a shot the client fires this much before
    /// its moment comes on the server, or this much after the skill is over there, is still the skill's, and another skill may
    /// start this much before the moment the playing one may be cut.
    /// </summary>
    public const float ShotLeeway = 0.3f;

    /// <summary>
    /// The slowest and the fastest attack speed a skill is played at, as a share of the normal one.
    /// </summary>
    public const float SlowestSpeed = 0.25f;

    public const float FastestSpeed = 4f;

    private readonly SkillAnimations _animations;
    private readonly float _speed;
    private readonly List<SkillHit> _pending = [];

    /// <summary>
    /// The shots (projectiles and chains) of the animations played so far whose moment has not come yet, in the order of time.
    /// </summary>
    private readonly List<SkillHit> _coming = [];

    /// <summary>
    /// The shots whose moment has come and that the client has not fired yet, by attack. Below zero when the client has fired a
    /// shot of the animation that comes next before it began here.
    /// </summary>
    private readonly Dictionary<int, int> _shots = [];

    private readonly Dictionary<ActionAnimation, ClipClock> _clocks = new(ReferenceEqualityComparer.Instance);

    private float _doneAt;
    private bool _pressed;
    private float _stepStart;
    private float _stepEnd;
    private float _nextRound;
    private bool _roundsEnded;
    private float _loopStart;
    private float _roundStart;
    private float _cutUntil = float.NegativeInfinity;
    private ActionAnimation? _clip;

    /// <param name="speed">The attack speed of the player as a share of the normal one: 1.2 plays the animations a fifth faster.</param>
    public SkillPlayback(SkillAnimations animations, float speed = 1f)
    {
        _animations = animations;
        _pressed = animations.StartsPressed;
        _speed = float.IsFinite(speed) && speed > 0f ? Math.Clamp(speed, SlowestSpeed, FastestSpeed) : 1f;

        Enter(animations.Start is null ? SkillStep.End : SkillStep.Start, 0f);
    }

    public SkillStep Step { get; private set; }

    public bool IsDone => Step == SkillStep.Done;

    /// <summary>
    /// The animation that plays now and the moment it began, in seconds from the start of the skill. Nothing once the skill is
    /// over or when the step has no animation.
    /// </summary>
    public (ActionAnimation Clip, float Began)? Playing => !IsDone && _clip is { } clip ? (clip, _stepStart) : null;

    /// <summary>
    /// When something happens next: an attack is judged, a shot comes due, or a step is over. Null once the skill has ended.
    /// </summary>
    public float? NextTime
    {
        get
        {
            if (IsDone)
            {
                return null;
            }

            var next = _stepEnd;

            if (_pending.Count > 0)
            {
                next = Math.Min(next, _pending[0].Time);
            }

            if (!_roundsEnded)
            {
                next = Math.Min(next, _nextRound);
            }

            return next;
        }
    }

    /// <summary>
    /// The attacks that are judged up to this moment, in the order of their time.
    /// </summary>
    public List<SkillHit> Advance(float now)
    {
        var due = new List<SkillHit>();

        while (!IsDone)
        {
            Round(now);

            while (_pending.Count > 0 && _pending[0].Time <= now)
            {
                due.Add(_pending[0]);
                _pending.RemoveAt(0);
            }

            // A shot comes due at its own moment, and only while its animation plays.
            var until = Math.Min(now, _stepEnd);

            while (_coming.Count > 0 && _coming[0].Time <= until)
            {
                _shots[_coming[0].Trigger.Id] = _shots.GetValueOrDefault(_coming[0].Trigger.Id) + 1;
                _coming.RemoveAt(0);
            }

            if (_stepEnd > now)
            {
                break;
            }

            Leave(_stepEnd);
        }

        return due;
    }

    /// <summary>
    /// The key is pressed or let go. <see cref="Advance"/> has to run up to this moment first.
    /// </summary>
    public void Press(bool pressed, float now)
    {
        if (pressed == _pressed)
        {
            return;
        }

        _pressed = pressed;

        // The wait is over once the player holds the key, and a loop is over once the key is let go.
        if (pressed && Step == SkillStep.Extra && _animations.Loop is not null)
        {
            Enter(SkillStep.Loop, now);
        }
        else if (!pressed && Step == SkillStep.Loop)
        {
            Enter(SkillStep.End, now);
        }
    }

    /// <summary>
    /// A blow of the skill has landed and holds the animation for the given time, as it holds the animation on the client:
    /// everything that was to come comes that much later. <see cref="Advance"/> has to run up to this moment first.
    /// </summary>
    public void Pause(float seconds)
    {
        if (IsDone || !float.IsFinite(seconds) || seconds <= 0f)
        {
            return;
        }

        _stepStart += seconds;
        _stepEnd += seconds;
        _roundStart += seconds;
        _loopStart += seconds;

        if (!_roundsEnded)
        {
            _nextRound += seconds;
        }

        Shift(_pending, seconds);
        Shift(_coming, seconds);
    }

    private static void Shift(List<SkillHit> list, float seconds)
    {
        for (var i = 0; i < list.Count; i++)
        {
            list[i] = list[i] with { Time = list[i].Time + seconds };
        }
    }

    /// <summary>
    /// Whether another skill may start now and cut this one, as the client lets the player: once the animation that plays has
    /// reached the moment it may be cut (or within <see cref="ShotLeeway"/> of it, or of the animation that begins next), a
    /// moment after the animation that was cut-able has given way to the next, and when the skill is over or about to be.
    /// <see cref="Advance"/> has to run up to this moment first.
    /// </summary>
    public bool MayCut(float now)
    {
        if (IsDone || now <= _cutUntil)
        {
            return true;
        }

        var ahead = now + ShotLeeway;

        if (_clip is { } clip && _roundStart + Clock(clip).CutOpens <= ahead)
        {
            return true;
        }

        var (next, began) = Following();

        return began <= ahead && (next is null || began + Clock(next).CutOpens <= ahead);
    }

    /// <summary>
    /// Whether the skill is over, or ends within <see cref="ShotLeeway"/> with nothing after it, so that the client may already be
    /// done with it. <see cref="Advance"/> has to run up to this moment first.
    /// </summary>
    public bool IsEnding(float now)
    {
        if (IsDone)
        {
            return true;
        }

        var (next, began) = Following();

        return next is null && began <= now + ShotLeeway;
    }

    private void Leave(float at)
    {
        switch (Step)
        {
            case SkillStep.Start:
                if (_animations.Loop is null)
                {
                    Enter(SkillStep.End, at);
                }
                else if (_animations.Extra is null || _pressed)
                {
                    Enter(SkillStep.Loop, at);
                }
                else
                {
                    Enter(SkillStep.Extra, at);
                }

                break;

            case SkillStep.Extra:
                // Nothing came of the wait.
                Enter(_pressed && _animations.Loop is not null ? SkillStep.Loop : SkillStep.End, at);
                break;

            case SkillStep.Loop:
                Enter(SkillStep.End, at);
                break;

            default:
                KeepCutOpen(at);
                Step = SkillStep.Done;
                _doneAt = at;
                _pending.Clear();
                _coming.Clear();
                break;
        }
    }

    /// <summary>
    /// The client fires a shot of the skill: a projectile or a chain. Each play of an animation, and each round of one that
    /// repeats, lets go of the shots it holds once, each at its own moment, and the skill takes no more of them than were let go.
    /// A shot whose moment comes within <see cref="ShotLeeway"/>, in the animation that plays or in the one that begins next, is
    /// taken early. <see cref="Advance"/> has to run up to this moment first.
    /// </summary>
    /// <returns>Whether the shot is the skill's to fire now.</returns>
    public bool TakeShot(int attack, float now)
    {
        if (IsDone && now > _doneAt + ShotLeeway)
        {
            return false;
        }

        var left = _shots.GetValueOrDefault(attack);

        if (left > 0)
        {
            _shots[attack] = left - 1;

            return true;
        }

        var ahead = now + ShotLeeway;

        // A shot of the animation that plays whose moment is near. One fired early before the animation began pays that back first.
        for (var i = 0; i < _coming.Count && _coming[i].Time <= ahead; i++)
        {
            if (_coming[i].Trigger.Id != attack)
            {
                continue;
            }

            _coming.RemoveAt(i--);

            if (left >= 0)
            {
                return true;
            }

            _shots[attack] = ++left;
        }

        if (IsDone)
        {
            return false;
        }

        // A shot of the animation that begins next, as things stand, whose moment is near.
        var (next, began) = Following();

        if (next is null || began > ahead)
        {
            return false;
        }

        var clock = Clock(next);
        var near = next.Attacks.Count(e => e.Id == attack && IsShot(e) && began + clock.Wall(e.Start) <= ahead);

        if (left > -near)
        {
            _shots[attack] = left - 1;

            return true;
        }

        return false;
    }

    /// <summary>
    /// The animation that begins next as things stand and the moment it begins: the next round of one that repeats, or the step that
    /// follows this one. No animation when the skill ends there.
    /// </summary>
    private (ActionAnimation? Clip, float Began) Following()
    {
        if (!_roundsEnded && _nextRound < _stepEnd)
        {
            return (_clip, _nextRound);
        }

        var clip = Step switch
        {
            SkillStep.Start when _animations.Loop is null => EndClip(_stepEnd),
            SkillStep.Start when _animations.Extra is null || _pressed => _animations.Loop,
            SkillStep.Start => _animations.Extra,
            SkillStep.Extra when _pressed && _animations.Loop is not null => _animations.Loop,
            SkillStep.Extra or SkillStep.Loop => EndClip(_stepEnd),
            _ => null
        };

        return (clip, _stepEnd);
    }

    private static bool IsShot(AttackTrigger attack)
    {
        return attack.IsProjectile || attack.IsChain;
    }

    private void Enter(SkillStep step, float at)
    {
        KeepCutOpen(at);

        Step = step;
        _stepStart = at;
        _roundStart = at;
        _pending.Clear();
        _coming.Clear();
        _roundsEnded = true;

        var clip = step switch
        {
            SkillStep.Start => _animations.Start,
            SkillStep.Loop => _animations.Loop,
            SkillStep.Extra => _animations.Extra,
            _ => EndClip(at)
        };

        _clip = clip;

        if (clip is null)
        {
            _stepEnd = at;
            return;
        }

        if (step == SkillStep.Loop)
        {
            _loopStart = at;
        }

        _stepEnd = at + Duration(step, clip);

        if (step is SkillStep.Loop or SkillStep.Extra && clip.Repeats && clip.Length > 0f)
        {
            // A clip that repeats judges its attacks again each time round.
            _roundsEnded = false;
            _nextRound = at;
        }
        else
        {
            AddAttacks(clip, at, float.PositiveInfinity);
        }
    }

    /// <summary>
    /// The animation that gives way at this moment had reached the moment it may be cut: the client, a little behind, may still be
    /// in it and cut it, so another skill may start for a moment more.
    /// </summary>
    private void KeepCutOpen(float at)
    {
        if (_clip is { } clip && _roundStart + Clock(clip).CutOpens <= at)
        {
            _cutUntil = at + ShotLeeway;
        }
    }

    private float Duration(SkillStep step, ActionAnimation clip)
    {
        var length = Clock(clip).Length;

        if (step == SkillStep.Extra)
        {
            return clip.Repeats ? WaitTime : length;
        }

        if (step != SkillStep.Loop || !clip.Repeats)
        {
            return length;
        }

        if (_animations.LoopTime > 0f)
        {
            return _animations.LoopTime;
        }

        // Held for as long as the player wants, and once round if the key is not held at all.
        return _pressed ? LongestHold : length;
    }

    /// <summary>
    /// The end of a charged skill is the one for the level the charge has reached.
    /// </summary>
    private ActionAnimation? EndClip(float at)
    {
        var steps = _animations.ChargeSteps;

        if (steps.Count == 0 || _loopStart > at)
        {
            return _animations.End;
        }

        var held = at - _loopStart;
        var level = 0;

        while (level < steps.Count - 1 && held >= steps[level])
        {
            held -= steps[level];
            level++;
        }

        return level < _animations.ChargedEnds.Count && _animations.ChargedEnds[level] is { } charged ? charged : _animations.End;
    }

    /// <summary>
    /// Brings in the rounds of a repeating clip that have begun by this time.
    /// </summary>
    private void Round(float now)
    {
        var clip = Step == SkillStep.Loop ? _animations.Loop : Step == SkillStep.Extra ? _animations.Extra : null;

        while (!_roundsEnded && clip is not null && _nextRound <= now)
        {
            if (_nextRound > _roundStart)
            {
                KeepCutOpen(_nextRound);
            }

            _roundStart = _nextRound;

            AddAttacks(clip, _nextRound, _stepEnd);

            _nextRound += Clock(clip).Length;

            if (_nextRound >= _stepEnd)
            {
                _roundsEnded = true;
            }
        }
    }

    /// <param name="limit">Attacks that would come after it are cut off with the step.</param>
    private void AddAttacks(ActionAnimation clip, float at, float limit)
    {
        var clock = Clock(clip);

        foreach (var attack in clip.Attacks)
        {
            var time = at + clock.Wall(attack.Start);

            if (time >= limit)
            {
                continue;
            }

            // The client fires the shots itself; the play only lets them go, each at its moment.
            if (IsShot(attack))
            {
                _coming.Add(new SkillHit(time, attack));
            }
            else if (attack.HitsArea || attack.IsTrap)
            {
                _pending.Add(new SkillHit(time, attack));
            }
        }

        _pending.Sort((a, b) => a.Time.CompareTo(b.Time));
        _coming.Sort((a, b) => a.Time.CompareTo(b.Time));
    }

    private ClipClock Clock(ActionAnimation clip)
    {
        if (!_clocks.TryGetValue(clip, out var clock))
        {
            clock = new ClipClock(clip, _speed);
            _clocks.Add(clip, clock);
        }

        return clock;
    }

    /// <summary>
    /// How the time of an animation goes by on the clock: at the attack speed of the player, and at their own speed in the
    /// stretches that have one. A stretch that begins while another lasts takes its place, and once it is over the animation
    /// plays at the attack speed again, as on the client.
    /// </summary>
    internal sealed class ClipClock
    {
        private readonly List<(float From, float To, float Speed)> _stretches = [];
        private readonly float _speed;

        public ClipClock(ActionAnimation clip, float speed)
        {
            _speed = speed;

            var events = (clip.Speeds ?? [])
                .Where(e => !e.Filtered && float.IsFinite(e.Speed) && e.Speed > 0f && float.IsFinite(e.Start) && float.IsFinite(e.End) && e.End > e.Start)
                .OrderBy(e => e.Start)
                .ToList();

            for (var i = 0; i < events.Count; i++)
            {
                var to = i + 1 < events.Count ? Math.Min(events[i].End, events[i + 1].Start) : events[i].End;

                if (to > events[i].Start)
                {
                    _stretches.Add((events[i].Start, to, events[i].Speed));
                }
            }

            Length = Wall(clip.Length);
            CutOpens = clip.Cuts is { Count: > 0 } cuts ? Wall(cuts.Min(e => e.Opens)) : float.PositiveInfinity;
        }

        /// <summary>
        /// How long the animation lasts on the clock.
        /// </summary>
        public float Length { get; }

        /// <summary>
        /// When, on the clock from the start of the animation, another skill may first cut it. Never for one that has no moment for it.
        /// </summary>
        public float CutOpens { get; }

        /// <summary>
        /// The moment on the clock, from the start of the animation, at which the animation reaches the given time of its own.
        /// </summary>
        public float Wall(float time)
        {
            var wall = 0f;
            var at = 0f;

            foreach (var (from, to, speed) in _stretches)
            {
                if (time <= from)
                {
                    break;
                }

                wall += Math.Max(0f, from - at) / _speed + (Math.Min(time, to) - Math.Max(from, at)) / speed;
                at = Math.Min(time, to);
            }

            return wall + Math.Max(0f, time - at) / _speed;
        }
    }
}
