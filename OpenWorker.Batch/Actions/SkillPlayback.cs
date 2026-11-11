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
/// All times count from the moment the skill began.
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

    private readonly SkillAnimations _animations;
    private readonly List<SkillHit> _pending = [];
    private bool _pressed;
    private float _stepStart;
    private float _stepEnd;
    private float _nextRound;
    private bool _roundsEnded;
    private float _loopStart;
    private ActionAnimation? _clip;

    public SkillPlayback(SkillAnimations animations)
    {
        _animations = animations;
        _pressed = animations.StartsPressed;

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
    /// When something happens next: an attack is judged, or a step is over. Null once the skill has ended.
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
                Step = SkillStep.Done;
                _pending.Clear();
                break;
        }
    }

    private void Enter(SkillStep step, float at)
    {
        Step = step;
        _stepStart = at;
        _pending.Clear();
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

    private float Duration(SkillStep step, ActionAnimation clip)
    {
        if (step == SkillStep.Extra)
        {
            return clip.Repeats ? WaitTime : clip.Length;
        }

        if (step != SkillStep.Loop || !clip.Repeats)
        {
            return clip.Length;
        }

        if (_animations.LoopTime > 0f)
        {
            return _animations.LoopTime;
        }

        // Held for as long as the player wants, and once round if the key is not held at all.
        return _pressed ? LongestHold : clip.Length;
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
            AddAttacks(clip, _nextRound, _stepEnd);

            _nextRound += clip.Length;

            if (_nextRound >= _stepEnd)
            {
                _roundsEnded = true;
            }
        }
    }

    /// <param name="limit">Attacks that would come after it are cut off with the step.</param>
    private void AddAttacks(ActionAnimation clip, float at, float limit)
    {
        foreach (var attack in clip.Attacks.Where(e => e.HitsArea || e.IsTrap))
        {
            var time = at + attack.Start;

            if (time < limit)
            {
                _pending.Add(new SkillHit(time, attack));
            }
        }

        _pending.Sort((a, b) => a.Time.CompareTo(b.Time));
    }
}
