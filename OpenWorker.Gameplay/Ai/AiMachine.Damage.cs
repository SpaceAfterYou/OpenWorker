using System.Numerics;

namespace OpenWorker.Gameplay.Ai;

/// <summary>
/// What a monster does when it is hurt. The blow adds to the hate of the one who dealt it, to the points of its escape and to
/// its counters; the monster looks at whoever has angered it most, unless a taunt holds it; and the monsters of its group
/// come to its help.
/// </summary>
public sealed partial class AiMachine
{
    /// <summary>
    /// What a blow adds to the escape points when it follows another within <see cref="QuickBlowTime"/>, from the first such
    /// blow on.
    /// </summary>
    private const float QuickBlowPoints = 0.2f;

    /// <summary>
    /// Blows this close together (in seconds) count for little.
    /// </summary>
    private const float QuickBlowTime = 0.5f;

    /// <summary>
    /// The hate the monsters of a group take on for the enemy of the one that called them.
    /// </summary>
    private const float AlertHate = 1f;

    /// <summary>
    /// How much of its hate a monster keeps at each look at its enemies, and how much of it is lost with time.
    /// </summary>
    private const float HateKept = 0.5f;

    private float _escapePoints = -1f;
    private float _sinceEscapePoint;
    private bool _escapeSawQuickBlow;
    private int _taunt = NoTarget;

    /// <summary>
    /// The monsters of this one's group are called when it has an enemy and is hurt or has just seen one.
    /// </summary>
    public bool CallsGroup { get; set; }

    /// <summary>
    /// The actor a taunt holds the monster on, or nothing.
    /// </summary>
    public int TauntTarget => _taunt;

    /// <summary>
    /// Somebody has hurt the monster. A monster that has no enemy yet turns on the one who hurt it.
    /// </summary>
    public void OnDamaged(int attacker, float aggro)
    {
        OnDamaged(new AiHit(attacker, aggro, 1));
    }

    /// <summary>
    /// A blow lands, in the order the original takes it: the escape points, the enemy of a monster that has none, the hate, then
    /// the counters.
    /// </summary>
    public void OnDamaged(in AiHit hit)
    {
        CountEscapeBlow(hit.Kind);

        if (_targetId == NoTarget && _host.GetTarget(hit.Attacker) is not null)
        {
            ChangeTarget(hit.Attacker);
            ChangeState(AiState.SelectAction);
        }

        if (_host.Health <= 0)
        {
            return;
        }

        AddAggro(hit.Attacker, hit.Hate);

        if (hit.Damage <= 0)
        {
            return;
        }

        _preSkillDamageCount++;
        _hitCount++;

        if (_targetId != NoTarget && CallsGroup)
        {
            AlertGroup();
        }

        _lastDamageTime = 0f;
    }

    /// <summary>
    /// Something other than a blow angers the monster at somebody: its hate for them grows, and a monster that has no enemy yet
    /// turns on them. It is no blow, so it adds to no counter and to no escape points.
    /// </summary>
    public void Provoke(int target, float hate)
    {
        if (_targetId == NoTarget && _host.GetTarget(target) is not null)
        {
            ChangeTarget(target);
            ChangeState(AiState.SelectAction);
        }

        if (_host.Health > 0)
        {
            AddAggro(target, hate);
        }
    }

    /// <summary>
    /// The monster is turned on somebody for as long as a taunt lasts: it fights them whatever hate says.
    /// </summary>
    public void Taunt(int target)
    {
        _taunt = target;

        if (_targetId != target && _host.GetTarget(target) is not null)
        {
            ChangeTarget(target);
            ChangeState(AiState.SelectAction);
        }
    }

    /// <summary>
    /// A taunt is over: hate decides again.
    /// </summary>
    public void ReleaseTaunt()
    {
        _taunt = NoTarget;
        _aggroChanged = true;
    }

    /// <summary>
    /// A monster of the same group fights somebody: this one, if it has no enemy, takes the same one.
    /// </summary>
    public void Alert(int target)
    {
        CallsGroup = false;

        if (_targetId != NoTarget)
        {
            return;
        }

        AddAggro(target, AlertHate);
        ChooseEnemyByHate();
        ChangeState(AiState.SelectAction);
    }

    /// <summary>
    /// Calls the monsters of the group once: a monster that has called them does not call them again.
    /// </summary>
    private void AlertGroup()
    {
        CallsGroup = false;

        _host.AlertGroup(_targetId);
    }

    /// <summary>
    /// The enemy who has angered the monster most is the one it looks at. Every look weakens the hate of all of them, with time
    /// and by half, and takes away what is left of the little there is.
    /// </summary>
    private void ChooseEnemyByHate()
    {
        _aggroChanged = false;

        var best = NoTarget;
        var most = 0f;
        var reset = _profile.DamageAggroResetTime;

        foreach (var (id, aggro) in _aggro.ToArray())
        {
            if (_host.GetTarget(id) is null)
            {
                _aggro.Remove(id);

                continue;
            }

            if (aggro.Value > most)
            {
                most = aggro.Value;
                best = id;
            }

            var left = (float)(_now - aggro.Time);

            if (left > 0f)
            {
                aggro.Value = reset <= left ? 0f : aggro.Value - aggro.Value * (left / reset);
            }

            aggro.Value -= aggro.Value * HateKept;

            if (reset >= aggro.Value)
            {
                aggro.Value = 0f;
            }
        }

        if (best == NoTarget || best == _targetId)
        {
            if (best == NoTarget && _profile.ClearsTarget)
            {
                ClearTarget();
            }

            return;
        }

        ChangeTarget(best);
    }

    /// <summary>
    /// The hate for the one the monster fights is forgotten when they have given none for the time the script says or have
    /// gone farther than it says; the monster then looks round for the enemy it hates most.
    /// </summary>
    private void ForgetCalmEnemy()
    {
        var time = _profile.DamageAggroResetTime;
        var distance = _profile.DamageAggroResetDistance;

        if (time <= 0f && distance <= 0f || !_aggro.TryGetValue(_targetId, out var aggro))
        {
            return;
        }

        var forgotten = false;

        if (time <= 0f || _now <= aggro.Time + time)
        {
            if (distance > 0f)
            {
                forgotten = Target is not { } target || Vector3.Distance(_host.Position, target.Position) > distance;
            }
        }
        else
        {
            forgotten = true;
        }

        if (forgotten)
        {
            _aggro.Remove(_targetId);

            ChooseEnemyByHate();
        }
    }

    // ---- Escape ---------------------------------------------------------------------------------

    /// <summary>
    /// The super armor of the monster is whole again: the skill its script names, if there is one, is played.
    /// </summary>
    public void OnSuperArmorRestored()
    {
        if (_profile.SuperArmorSkill > 0 && _host.FindSkill(_profile.SuperArmorSkill) is { } skill)
        {
            _host.UseSkill(skill, Target ?? Self);
        }
    }

    /// <summary>
    /// A blow adds to the points of the escape: a skill a whole point, a projectile half of one, a trap a fifth. From the first
    /// time two blows follow each other within half a second every blow of the monster counts a fifth, as in the original. At
    /// the points the script asks for, the monster escapes with the chance it has.
    /// </summary>
    private void CountEscapeBlow(AiHitKind kind)
    {
        if (_profile.Follower.Escape is not { Points: > 0f } escape || IsRecovering)
        {
            return;
        }

        if (_escapePoints < 0f)
        {
            _escapePoints = RandomBetween(0f, escape.Points / 2f);
        }

        if (!_escapeSawQuickBlow && _lastDamageTime is >= 0f and < QuickBlowTime)
        {
            _escapeSawQuickBlow = true;
        }

        var points = _escapeSawQuickBlow
            ? QuickBlowPoints
            : kind switch
            {
                AiHitKind.Projectile => 0.5f,
                AiHitKind.Trap => 0.2f,
                _ => 1f
            };

        _escapePoints += points;
        _sinceEscapePoint = 0f;

        if (_escapePoints < escape.Points)
        {
            return;
        }

        _escapePoints = RandomBetween(0f, escape.Points / 2f);

        if (escape.Chance <= _random.Next(RandomRange))
        {
            return;
        }

        var seconds = 0f;

        switch (escape.Kind)
        {
            case AiEscape.SuperArmorKind:
                seconds = escape.Value * 0.001f;

                // A monster whose armor is broken does not get it back by escaping: the armor mends in its own time.
                if (!_host.IsArmorBroken)
                {
                    _host.ForceDefense(AiDefense.SuperArmor, seconds);
                }

                _host.EndFlinch();
                break;

            case AiEscape.InvincibleKind:
                seconds = escape.Value * 0.001f;

                _host.ForceDefense(AiDefense.Invincible, seconds);
                _host.EndFlinch();
                break;

            case AiEscape.SkillKind:
                if (_host.IsDownOrFlying || _host.FindSkill(escape.Value) is not { } skill)
                {
                    return;
                }

                _host.UseSkill(skill, Target ?? Self);
                break;

            default:
                return;
        }

        _host.NotifyEscape(seconds);
    }

    /// <summary>
    /// The points of the escape start again after the time the script says without a blow.
    /// </summary>
    private void StepEscape(float elapsed)
    {
        if (_escapePoints <= 0f || _profile.Follower.Escape is not { } escape)
        {
            return;
        }

        _sinceEscapePoint += elapsed;

        if (_sinceEscapePoint >= escape.ResetTime)
        {
            _escapePoints = RandomBetween(0f, escape.Points / 2f);
            _sinceEscapePoint = 0f;
        }
    }
}
