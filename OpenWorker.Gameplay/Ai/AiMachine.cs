using System.Numerics;

namespace OpenWorker.Gameplay.Ai;

/// <summary>
/// The mind of one monster: a machine of states that a script describes. It thinks every <see cref="AiProfile.CheckTime"/>
/// seconds, looks at the transitions of its state and takes the first one whose conditions hold.
/// </summary>
public sealed partial class AiMachine
{
    public const int NoTarget = -1;

    private const int HistorySize = 16;
    private const int MaxNesting = 8;
    private const float SkillGiveUpTime = 30f;
    private const float RootedGiveUpTime = 5f;
    private const float ReturnGiveUpTime = 30f;
    private const float RangeSlack = 30f;
    private const float DefaultSkillAngle = 45f;
    private const float RetargetPause = 0.2f;
    private const float RepathPause = 0.3f;
    private const float ReturnArrival = 50f;
    private const int RandomRange = 10000;
    private const float AggroPerSighting = 1f;

    private readonly AiProfile _profile;
    private readonly IAiHost _host;
    private readonly Random _random;
    private readonly List<AiState> _history = [];
    private readonly Dictionary<AiTransition, TransitionTimers> _timers = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<AiCondition, double> _rolled = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<int, Aggro> _aggro = [];
    private readonly Dictionary<AiState, double> _stateEnded = [];
    private readonly Dictionary<int, double> _cooldowns = [];
    private readonly int[] _skillOrder = new int[AiProfile.SkillSlots];
    private readonly Dictionary<AiState, List<AiTransition>> _ordered = [];

    private double _now;
    private double _globalCooldownEnd;
    private AiState _state;
    private float _sinceCheck;
    private float _stateTime;
    private float _lifeTime = -1f;
    private float _activeTime;
    private float _searchTime;
    private float _retargetTime;
    private float _repathTime;
    private float _aggroTime;
    private float _lastSkillTime = -1f;
    private float _lastDamageTime = -1f;
    private int _statePreviousHealth;
    private int _preSkillDamageCount;
    private int _hitCount;
    private int _previousRandom = 1;
    private int _targetId = NoTarget;
    private int _nesting;
    private int _runawayCount;
    private AiStateSettings _settings = new();
    private Vector3 _moveStart;
    private float _actionEnd;

    private AiSkill? _skill;
    private int _skillSlot = -1;
    private int _skillGroup = -1;
    private bool _skillStarted;
    private bool _aggroChanged;
    private bool _gazing;

    public AiMachine(AiProfile profile, IAiHost host, Random? random = null)
    {
        _profile = profile;
        _host = host;
        _random = random ?? Random.Shared;
    }

    public AiState State => _state;

    /// <summary>
    /// The monster has left a state for another: the old state and the new one.
    /// </summary>
    public event Action<AiState, AiState>? StateChanged;

    public int TargetId => _targetId;

    public AiProfile Profile => _profile;

    /// <summary>
    /// The number the state has been told to take: the skill slot or the group the monster uses right now.
    /// </summary>
    public int SelectedSkillSlot => _skillSlot;

    /// <summary>
    /// How much each enemy has done to anger the monster.
    /// </summary>
    public IEnumerable<KeyValuePair<int, float>> AggroList => _aggro.Select(e => new KeyValuePair<int, float>(e.Key, e.Value.Value));

    private sealed class TransitionTimers
    {
        public float Timer;
        public float Attack;
        public float Move;
    }

    private sealed class Aggro
    {
        public float Value;
        public double Time;
    }

    /// <summary>
    /// The monster has just come to life.
    /// </summary>
    public void Start(AiState initial = AiState.Wait)
    {
        _state = initial;
        _history.Clear();
        _history.Add(initial);
        _statePreviousHealth = _host.Health;

        if (_profile.States.Contains(initial))
        {
            BeginState();
            Enter(initial);
        }
        else
        {
            BeginState();
        }
    }

    /// <summary>
    /// Moves the monster on by a time.
    /// </summary>
    public void Update(float elapsed)
    {
        if (_state == AiState.None || _host.IsDead || elapsed <= 0f)
        {
            return;
        }

        _now += elapsed;
        _sinceCheck += elapsed;
        _stateTime += elapsed;
        _activeTime += elapsed;

        if (_host.CanAttack || _host.CanMove)
        {
            foreach (var transition in _profile.GetTransitions(_state))
            {
                var timers = TimersOf(transition);

                if (_host.CanAttack)
                {
                    timers.Attack += elapsed;
                }

                if (_host.CanMove)
                {
                    timers.Move += elapsed;
                }
            }
        }

        if (_lastSkillTime >= 0f)
        {
            _lastSkillTime += elapsed;
        }

        if (_lastDamageTime >= 0f)
        {
            _lastDamageTime += elapsed;
        }

        _aggroTime += elapsed;
        _retargetTime += elapsed;
        _repathTime += elapsed;

        StepEscape(elapsed);

        if (_aggroChanged && _aggroTime >= _profile.AggroCheckTime)
        {
            _aggroTime = 0f;
            ChooseEnemyByHate();
        }

        if (_sinceCheck > _profile.CheckTime)
        {
            var elapsedSinceCheck = _sinceCheck;
            var changed = Evaluate(elapsedSinceCheck);

            _sinceCheck = 0f;

            if (changed)
            {
                BeginState();
            }
            else
            {
                CheckLifeTime();
            }
        }
        else
        {
            CheckLifeTime();
        }
    }

    /// <summary>
    /// The monster is made to take a state, as a script does it.
    /// </summary>
    public void Force(AiState state)
    {
        ChangeState(state);
    }

    public void ForgetTarget()
    {
        ClearTarget();
    }

    private TransitionTimers TimersOf(AiTransition transition)
    {
        if (!_timers.TryGetValue(transition, out var timers))
        {
            timers = new TransitionTimers();
            _timers.Add(transition, timers);
        }

        return timers;
    }

    private IReadOnlyList<AiTransition> TransitionsOf(AiState state)
    {
        return _ordered.TryGetValue(state, out var list) ? list : _profile.GetTransitions(state);
    }

    // ---- Machine ------------------------------------------------------------------------------

    private bool Has(AiState state) => _profile.States.Contains(state);

    /// <summary>
    /// Takes a state and starts it. The state is ignored when the monster does not have it.
    /// </summary>
    private void ChangeState(AiState state)
    {
        if (_nesting >= MaxNesting)
        {
            return;
        }

        _nesting++;

        try
        {
            SetState(state);
            BeginState();
        }
        finally
        {
            _nesting--;
        }
    }

    private bool SetState(AiState state)
    {
        if (!Has(state))
        {
            return false;
        }

        var old = _state;

        Leave(old);

        _state = state;

        _history.Add(state);

        if (_history.Count > HistorySize)
        {
            _history.RemoveAt(0);
        }

        BeginState();
        StateChanged?.Invoke(old, state);
        Enter(state);

        return true;
    }

    private void GoBack(AiState from)
    {
        if (_history.Count > 0)
        {
            _history.RemoveAt(_history.Count - 1);
        }

        var previous = _history.Count > 0 ? _history[^1] : AiState.Wait;

        if (_history.Count == 0)
        {
            _history.Add(AiState.Wait);
        }

        _state = previous;

        if (Has(from))
        {
            Leave(from);
        }

        BeginState();
        StateChanged?.Invoke(from, previous);

        if (Has(previous))
        {
            Enter(previous);
        }
    }

    /// <summary>
    /// What the machine does when a state starts, before the state itself has its say.
    /// </summary>
    private void BeginState()
    {
        _stateTime = 0f;
        _lifeTime = -1f;
        _moveStart = _host.Position;
        _statePreviousHealth = _host.Health;
        _settings = _profile.GetSettings(_state);

        if (_settings.LifeMin >= 0)
        {
            _lifeTime = _settings.LifeMax <= 0
                ? _settings.LifeMin * 0.001f
                : RandomBetween(_settings.LifeMin, _settings.LifeMax) * 0.001f;
        }
    }

    private void Enter(AiState state)
    {
        foreach (var transition in TransitionsOf(state))
        {
            foreach (var condition in transition.Conditions)
            {
                if (condition.IsRandom)
                {
                    Roll(condition);
                }
            }

            if (transition.Conditions.Any(e => e.Variable == AiVariable.Timer))
            {
                TimersOf(transition).Timer = 0f;
            }
        }

        OnEntered(state);
    }

    private void Leave(AiState state)
    {
        _stateEnded[state] = _now;

        if (state is >= AiState.AttackSkill1 and <= AiState.AttackSkill10)
        {
            _skillStarted = false;
            _skill = null;
        }
        else if (state == AiState.Gaze)
        {
            StopGaze();
        }
        else if (state == AiState.Return)
        {
            if (_profile.ReturnHealth == 0)
            {
                _host.RestoreHealth();
            }

            _host.SetInvincible(false);
        }
        else if (state == AiState.Recovery)
        {
            EndRecovery();
        }
    }

    private void CheckLifeTime()
    {
        if (_lifeTime > 0f && _stateTime >= _lifeTime)
        {
            EndState();
        }
    }

    /// <summary>
    /// The time of a state is over: it chooses where to go, by the weights the script gives.
    /// </summary>
    private void EndState()
    {
        if (_state == AiState.SelectAction)
        {
            SelectAction();

            return;
        }

        var next = _settings.NextStates;
        var total = next.Sum(e => Math.Max(0, e.Weight));

        if (total == 0)
        {
            ChangeState(_settings.DefaultNext);

            return;
        }

        var roll = _random.Next(total);

        foreach (var (state, weight) in next)
        {
            if (roll < weight)
            {
                ChangeState(state);

                return;
            }

            roll -= weight;
        }

        ChangeState(AiState.BeforeState);
    }

    /// <summary>
    /// The monster in a fight picks what to do next from the weights of its select-action state.
    /// </summary>
    private void SelectAction()
    {
        var options = _profile.GetSettings(AiState.SelectAction).NextStates
            .Where(e => e.State is AiState.Attack or AiState.BattleMove or AiState.Runaway or AiState.Protection
                or AiState.Gaze or AiState.BattleWait or AiState.Move)
            .Where(e => e.Weight > 0)
            .ToArray();

        var skipRunaway = _profile.RunawayHealth > 0 && HealthPercent() > _profile.RunawayHealth
            || _runawayCount >= _profile.RunawayCount;

        options = options
            .Where(e => e.State != AiState.Protection && !(skipRunaway && e.State == AiState.Runaway))
            .ToArray();

        if (options.Length == 1)
        {
            ChangeState(options[0].State);

            return;
        }

        var total = options.Sum(e => e.Weight);

        if (total == 0)
        {
            ChangeState(AiState.BeforeState);

            return;
        }

        var roll = _random.Next(total);

        foreach (var (state, weight) in options)
        {
            if (roll < weight)
            {
                ChangeState(state);

                return;
            }

            roll -= weight;
        }
    }

    /// <summary>
    /// Looks for a way out of the state. Returns whether the state was left or the monster stayed by its own decision.
    /// </summary>
    private bool Evaluate(float elapsed)
    {
        var previous = _state;

        if (!Has(previous))
        {
            return false;
        }

        var transitions = TransitionsOf(previous);

        if (transitions.Count == 0 && !HasUpdate(previous))
        {
            GoBack(previous);

            return true;
        }

        foreach (var transition in transitions)
        {
            var timers = TimersOf(transition);

            timers.Timer += elapsed;

            if (!AreMet(transition, timers))
            {
                continue;
            }

            if (transition.Output == AiState.BeforeState)
            {
                GoBack(previous);
            }
            else if (previous != transition.Output)
            {
                SetState(transition.Output);
            }

            return true;
        }

        if (previous == _state)
        {
            Tick(previous, elapsed);
        }

        return false;
    }

    private static bool HasUpdate(AiState state) => state != AiState.Attack;

    private bool AreMet(AiTransition transition, TransitionTimers timers)
    {
        foreach (var condition in transition.Conditions)
        {
            if (AiCondition.IsTimer(condition.Variable))
            {
                var time = condition.Variable switch
                {
                    AiVariable.CanAttackTime => timers.Attack,
                    AiVariable.CanMoveTime => timers.Move,
                    _ => timers.Timer
                };

                if (!condition.IsMet(time, condition.Low))
                {
                    return false;
                }

                switch (condition.Variable)
                {
                    case AiVariable.CanAttackTime:
                        timers.Attack = 0f;
                        break;

                    case AiVariable.CanMoveTime:
                        timers.Move = 0f;
                        break;

                    default:
                        timers.Timer = 0f;
                        break;
                }

                continue;
            }

            if (!IsMet(condition))
            {
                return false;
            }

            if (condition.IsRandom)
            {
                Roll(condition);
            }
        }

        return true;
    }

    private bool IsMet(AiCondition condition)
    {
        var argument = AiCondition.TakesArgument(condition.Variable) ? (int)condition.High : 0;
        var low = condition.IsRandom ? Rolled(condition) : condition.Low;

        return condition.IsMet(Read(condition.Variable, argument), low);
    }

    private bool AreMet(IReadOnlyList<AiCondition> conditions)
    {
        return conditions.All(IsMet);
    }

    private double Rolled(AiCondition condition)
    {
        if (!_rolled.TryGetValue(condition, out var value))
        {
            value = Roll(condition);
        }

        return value;
    }

    private double Roll(AiCondition condition)
    {
        var low = Math.Min(condition.Low, condition.High);
        var high = Math.Max(condition.Low, condition.High);
        var value = AiCondition.IsInteger(condition.Variable)
            ? RandomBetween((int)low, (int)high)
            : low + _random.NextDouble() * (high - low);

        _rolled[condition] = value;

        return value;
    }

    private int RandomBetween(int low, int high)
    {
        return low >= high ? low : _random.Next(low, high + 1);
    }

    private float RandomBetween(float low, float high)
    {
        return low >= high ? low : low + (float)_random.NextDouble() * (high - low);
    }

    // ---- Variables ----------------------------------------------------------------------------

    private int HealthPercent()
    {
        return _host.MaxHealth <= 0 ? 0 : (int)(_host.Health * 100f / _host.MaxHealth);
    }

    private AiTarget? Target => _targetId == NoTarget ? null : _host.GetTarget(_targetId);

    private double Read(AiVariable variable, int argument)
    {
        switch (variable)
        {
            case AiVariable.IsTarget:
                return _targetId != NoTarget ? 1 : 0;

            case AiVariable.TargetDistance:
                return Target is { } target ? Vector3.Distance(_host.Position, target.Position) : -1;

            case AiVariable.TargetDistanceCapsule:
                return Target is { } body ? Vector3.Distance(_host.Position, body.Position) - body.Radius : -1;

            case AiVariable.TargetDirection:
                return Target is { } aim ? AiMath.AngleBetween(_host.Yaw, AiMath.YawTo(_host.Position, aim.Position)) : 0;

            case AiVariable.TargetLook:
                return Target is { } look ? AiMath.AngleBetween(look.Yaw, AiMath.YawTo(_host.Position, look.Position)) : 0;

            case AiVariable.CreationPositionDistance:
                return Vector3.Distance(_host.Position, _host.HomePosition);

            case AiVariable.StateTime:
                return _stateTime;

            case AiVariable.IsAttack:
                return _host.IsAttacking ? 1 : 0;

            case AiVariable.Health:
                return HealthPercent();

            case AiVariable.IsHealthChanged:
                return _host.Health != _statePreviousHealth ? 1 : 0;

            case AiVariable.Random:
                _previousRandom = _random.Next(RandomRange);

                return _previousRandom;

            case AiVariable.RandomPreviousValue:
                return _previousRandom;

            case AiVariable.IsMoving:
                return _host.IsMoving ? 1 : 0;

            case AiVariable.TargetHealth:
                return Target is { MaxHealth: > 0 } hurt ? (int)(hurt.Health * 100f / hurt.MaxHealth) : -1;

            case AiVariable.SpawnMonsterCount:
                return _host.CountSpawnedMonsters();

            case AiVariable.SectorMonsterCount:
            case AiVariable.MonsterCount:
                return _host.CountSectorMonsters();

            case AiVariable.PreSkillDamageCount:
                return _preSkillDamageCount;

            case AiVariable.LastSkillTime:
                return _lastSkillTime;

            case AiVariable.LastDamageTime:
                return _lastDamageTime;

            case AiVariable.SpawnTime:
                return _host.Age;

            case AiVariable.ActivateTime:
                return _activeTime;

            case AiVariable.GlobalCooldown:
                return (int)Math.Max(0, _globalCooldownEnd - _now);

            case AiVariable.GroupCooldown:
                return IsGroupCooling(argument) ? 1 : 0;

            case AiVariable.SkillCooldown:
                return IsSkillCooling(argument) ? 1 : 0;

            case AiVariable.StateEndTime:
                return _stateEnded.TryGetValue((AiState)argument, out var ended) ? _now - ended : -1;

            // Both count within the distance the script gives, and nothing without one.
            case AiVariable.EnemyCount:
                return argument > 0 ? _host.CountEnemies(argument) : 0;

            case AiVariable.UserCount:
                return argument > 0 ? _host.CountUsers(argument) : 0;

            case AiVariable.AggroCount:
                return _aggro.Count;

            case AiVariable.HitCount:
                return _hitCount;

            case AiVariable.GuardDistance:
            case AiVariable.GuardDistanceCapsule:
            case AiVariable.GuardHealth:
                return ReadGuard(variable);

            default:
                return 0;
        }
    }

    // ---- Targets and aggro --------------------------------------------------------------------

    private void AddAggro(int id, float value)
    {
        if (!_aggro.TryGetValue(id, out var aggro))
        {
            aggro = new Aggro();
            _aggro.Add(id, aggro);
        }

        aggro.Value += value;
        aggro.Time = _now;
        _aggroChanged = true;
    }

    private void ChangeTarget(int id)
    {
        // A taunt holds the monster on the one who gave it for as long as that one is there to be fought.
        if (_taunt != NoTarget)
        {
            if (_host.GetTarget(_taunt) is not null)
            {
                id = _taunt;
            }
            else
            {
                _taunt = NoTarget;
            }
        }

        if (_targetId == id)
        {
            return;
        }

        _targetId = id;

        _host.SetTarget(id);

        // A new target starts the waiting of the transitions of the state anew.
        if (_state == AiState.SelectAction)
        {
            foreach (var transition in TransitionsOf(_state))
            {
                var timers = TimersOf(transition);

                timers.Timer = 0f;
                timers.Attack = 0f;
                timers.Move = 0f;
            }
        }
    }

    private void ClearTarget()
    {
        ChangeTarget(NoTarget);
    }

    private void SearchTarget()
    {
        if (_profile.SightDistance <= 0f || _host.FindEnemy(_profile.SightDistance) is not { } enemy)
        {
            return;
        }

        if (enemy.Id != _targetId)
        {
            AddAggro(enemy.Id, AggroPerSighting);
            ChooseEnemyByHate();

            if (_targetId != NoTarget && CallsGroup)
            {
                AlertGroup();
            }
        }
    }

    private bool IsFarFromHome()
    {
        // A helper goes where its owner goes and never returns to where it appeared.
        if (IsFollower || Vector3.Distance(_host.Position, _host.HomePosition) <= _profile.ReturnDistance)
        {
            return false;
        }

        ChangeState(AiState.Return);

        return true;
    }

    // ---- Skills -------------------------------------------------------------------------------

    private double CooldownEnd(int group) => _cooldowns.GetValueOrDefault(group);

    private float Cooldown(int group)
    {
        var end = Math.Max(CooldownEnd(group), _globalCooldownEnd);

        return (float)Math.Max(0, end - _now);
    }

    private bool IsGroupCooling(int group)
    {
        if (group is < 0 or >= AiProfile.SkillSlots)
        {
            return false;
        }

        foreach (var slot in _profile.SkillGroups[group])
        {
            if (slot > 0 && _host.GetSkill(slot - 1) is { } skill && Cooldown(skill.CooldownGroup) > 0)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsSkillCooling(int skillId)
    {
        for (var slot = 0; slot < AiProfile.SkillSlots; slot++)
        {
            if (_host.GetSkill(slot) is { } skill && skill.Id == skillId)
            {
                return Cooldown(skill.CooldownGroup) > 0;
            }
        }

        return false;
    }

    private void StartCooldown(AiSkill skill)
    {
        if (skill.Cooldown <= 0f && skill.GlobalCooldown <= 0f)
        {
            return;
        }

        var end = _now + skill.Cooldown;

        _cooldowns[skill.CooldownGroup] = Math.Max(CooldownEnd(skill.CooldownGroup), end);
        _globalCooldownEnd = Math.Max(_globalCooldownEnd, _now + skill.GlobalCooldown);
    }

    private bool CanChoose(int slot, int group)
    {
        if (group is >= 0 and < AiProfile.SkillSlots && !AreMet(_profile.SkillGroupRules[group][slot].Conditions))
        {
            return false;
        }

        return AreMet(_profile.SkillRules[slot].Conditions);
    }

    /// <summary>
    /// The slot of the skill a group of skills uses now.
    /// </summary>
    private int ChooseSlot(int group)
    {
        if (!_profile.HasSkillGroups)
        {
            return group;
        }

        int[] order;

        switch (_profile.SkillSortTypes[group])
        {
            case 1:
                order = [0, 1, 2];
                _random.Shuffle(order);
                break;

            case 2:
                order = [_skillOrder[group], (_skillOrder[group] + 1) % 3, (_skillOrder[group] + 2) % 3];
                break;

            default:
                order = [0, 1, 2];
                break;
        }

        foreach (var position in order)
        {
            var number = _profile.SkillGroups[group][position];

            if (number <= 0 || number > AiProfile.SkillSlots)
            {
                continue;
            }

            var slot = number - 1;

            if (_host.GetSkill(slot) is { } skill && Cooldown(skill.CooldownGroup) <= 0f && CanChoose(slot, group))
            {
                if (_profile.SkillSortTypes[group] == 2)
                {
                    _skillOrder[group] = (position + 1) % 3;
                }

                return slot;
            }
        }

        if (_profile.SkillSortTypes[group] == 2)
        {
            _skillOrder[group] = 1;
        }

        // Nothing fits: the first skill is the one that is used.
        return 0;
    }

    private AiState ActionAfterSkill(int slot, int group)
    {
        if (group is < 0 or >= AiProfile.SkillSlots || slot is < 0 or >= AiProfile.SkillSlots)
        {
            return AiState.None;
        }

        var rule = _profile.ActionsAfterSkill[group][slot];

        return AreMet(rule.Conditions) ? rule.NextState : AiState.None;
    }

    /// <summary>
    /// The attack state orders its ways out anew each time: the groups of skills the script weighs come first.
    /// </summary>
    private void OrderAttacks()
    {
        if (!_profile.HasSkillGroups)
        {
            return;
        }

        var weights = _profile.SkillGroupRatios.ToArray();
        var order = new List<AiState>();
        var total = weights.Sum();

        while (total > 0)
        {
            var roll = _random.Next(total);

            for (var i = 0; i < weights.Length; i++)
            {
                if (roll < weights[i])
                {
                    order.Add(AiState.AttackSkill1 + i);
                    weights[i] = 0;

                    break;
                }

                roll -= weights[i];
            }

            total = weights.Sum();
        }

        var list = _profile.GetTransitions(AiState.Attack).ToList();

        for (var i = order.Count - 1; i >= 0; i--)
        {
            var index = list.FindIndex(e => e.Output == order[i]);

            if (index > 0)
            {
                var transition = list[index];

                list.RemoveAt(index);
                list.Insert(0, transition);
            }
        }

        _ordered[AiState.Attack] = list;
    }

    // ---- States -------------------------------------------------------------------------------

    private void OnEntered(AiState state)
    {
        switch (state)
        {
            case AiState.Wait:
                StartWait();
                break;

            case AiState.Patrol:
                StartPatrol();
                break;

            case AiState.SelectAction:
                StartSelectAction();
                break;

            case AiState.BattleMove:
                StartBattleMove();
                break;

            case AiState.Gaze:
                StartGaze();
                break;

            case AiState.Move:
                StartMove();
                break;

            case AiState.Attack:
                if (!IsFarFromOwner(true, false))
                {
                    OrderAttacks();
                }

                break;

            case >= AiState.AttackSkill1 and <= AiState.AttackSkill10:
                StartAttackSkill(state - AiState.AttackSkill1);
                break;

            case >= AiState.CommonAction1 and <= AiState.CommonAction10:
                StartCommonAction(state - AiState.CommonAction1);
                break;

            case AiState.Return:
                StartReturn();
                break;

            case AiState.Suicide:
                _host.Die();
                break;

            case AiState.Runaway:
                StartRunaway();
                break;

            case AiState.Recovery:
                StartRecovery();
                break;

            case AiState.TraceMove:
                StartTraceMove();
                break;
        }
    }

    private void Tick(AiState state, float elapsed)
    {
        switch (state)
        {
            case AiState.Wait:
                UpdateWait(elapsed);
                break;

            case AiState.Patrol:
            case AiState.BattleMove:
            case AiState.Gaze:
            case AiState.Runaway:
                UpdateMovement(state);
                break;

            case AiState.SelectAction:
                UpdateSelectAction(elapsed);
                break;

            case AiState.Move:
                UpdateMove();
                break;

            case >= AiState.AttackSkill1 and <= AiState.AttackSkill10:
                UpdateAttackSkill(elapsed);
                break;

            case >= AiState.CommonAction1 and <= AiState.CommonAction10:
                UpdateCommonAction();
                break;

            case AiState.Return:
                UpdateReturn();
                break;

            case AiState.Recovery:
                UpdateRecovery(elapsed);
                break;

            case AiState.TraceMove:
                UpdateFollowing();
                break;
        }
    }

    private void StartWait()
    {
        ClearTarget();

        _searchTime = 0f;
        _lastSkillTime = -1f;

        if (_host.IsBattlePose)
        {
            _host.SetBattlePose(false, true);
        }

        // Somebody who stands where he is looks around at once.
        _searchTime = _profile.SearchDelay;
    }

    private void UpdateWait(float elapsed)
    {
        if (!_profile.SeeksEnemies)
        {
            return;
        }

        _searchTime += elapsed;

        if (_searchTime <= _profile.SearchDelay)
        {
            return;
        }

        _searchTime = 0f;

        SearchTarget();

        if (_targetId != NoTarget)
        {
            ChangeState(AiState.SelectAction);
        }
    }

    private void StartSelectAction()
    {
        _host.SetBattlePose(true, true);

        if (_lastSkillTime < 0f)
        {
            _lastSkillTime = 0f;
        }

        if (_host.IsMoving)
        {
            _host.StopMoving(true);
        }

        UpdateSelectAction(0f);
    }

    private void UpdateSelectAction(float elapsed)
    {
        if (IsFarFromHome())
        {
            return;
        }

        var target = Target;

        if (target is null)
        {
            ClearTarget();
            ChangeState(AiState.Wait);

            return;
        }

        if (_profile.TargetLostDistance > 0f && Vector3.Distance(_host.Position, target.Value.Position) > _profile.TargetLostDistance)
        {
            ClearTarget();
            ChangeState(AiState.Wait);

            return;
        }

        if (_retargetTime > RetargetPause)
        {
            _retargetTime = 0f;

            ForgetCalmEnemy();

            if (_host.CanMove && Target is { } enemy)
            {
                _host.FaceTo(enemy.Position);
            }
        }
    }

    private void StartPatrol()
    {
        if (!_host.CanMove)
        {
            return;
        }

        var home = _host.HomePosition;
        var angle = RandomBetween(_settings.AngleMin, _settings.AngleMax) * (_random.Next(2) == 0 ? -1 : 1);
        var distance = RandomBetween(_settings.MoveDistanceMin, _settings.MoveDistanceMax);
        var direction = AiMath.Direction(_host.Yaw + angle);
        var destination = _host.Position + new Vector3(direction * distance, 0f);
        var range = _settings.TargetDistanceMax;

        if (range > 0f)
        {
            destination.X = Math.Clamp(destination.X, home.X - range, home.X + range);
            destination.Y = Math.Clamp(destination.Y, home.Y - range, home.Y + range);
        }

        destination.Z = _host.Position.Z;

        _host.SetBattlePose(false, false);

        if (!_host.MoveTo(destination, false))
        {
            ChangeState(AiState.Wait);
        }
    }

    /// <summary>
    /// Most movements end when the monster has arrived.
    /// </summary>
    private void UpdateMovement(AiState state)
    {
        if (state is AiState.BattleMove or AiState.Runaway && IsFarFromHome())
        {
            return;
        }

        if (state is AiState.BattleMove or AiState.Gaze or AiState.Runaway && Target is null)
        {
            ClearTarget();
            ChangeState(AiState.Wait);

            return;
        }

        if (_host.IsMoving)
        {
            return;
        }

        ChangeState(state == AiState.Patrol ? AiState.Wait : AiState.SelectAction);
    }

    private void StartBattleMove()
    {
        if (Target is not { } target)
        {
            ClearTarget();
            ChangeState(AiState.Wait);

            return;
        }

        if (_host.IsMoving)
        {
            _host.StopMoving(true);
        }

        StopGaze();

        // The monster goes to a place around the target: the distance and the angle from the side it stands on.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var destination = PlaceAround(target, _settings.TargetDistanceMin, _settings.TargetDistanceMax, _settings.AngleMin, _settings.AngleMax);

            if (_host.MoveTo(destination, true))
            {
                return;
            }
        }
    }

    private Vector3 PlaceAround(AiTarget target, float distanceMin, float distanceMax, float angleMin, float angleMax)
    {
        var away = new Vector2(_host.Position.X - target.Position.X, _host.Position.Y - target.Position.Y);

        if (away.LengthSquared() < 1f)
        {
            away = AiMath.Direction(_host.Yaw + 180f);
        }

        var angle = RandomBetween(angleMin, angleMax) * (_random.Next(2) == 0 ? -1 : 1);
        var distance = RandomBetween(distanceMin, distanceMax);
        var around = AiMath.Rotate(Vector2.Normalize(away), angle) * Math.Max(distance, target.Radius + _host.Radius);

        return new Vector3(target.Position.X + around.X, target.Position.Y + around.Y, target.Position.Z);
    }

    private void StartGaze()
    {
        if (Target is not { } target)
        {
            ClearTarget();
            ChangeState(AiState.Wait);

            return;
        }

        if (_host.IsMoving)
        {
            _host.StopMoving(true);
        }

        _gazing = true;
        _host.SetGaze(true);

        // Walks along a circle around the target, to one side.
        var offset = new Vector2(_host.Position.X - target.Position.X, _host.Position.Y - target.Position.Y);
        var radius = Math.Max(offset.Length(), 1f);
        var wanted = _settings.TargetDistanceMax > 0f ? RandomBetween(_settings.TargetDistanceMin, _settings.TargetDistanceMax) : radius;
        var arc = RandomBetween(_settings.MoveDistanceMin, _settings.MoveDistanceMax);
        var turn = arc / Math.Max(wanted, 1f) * (180f / MathF.PI) * (_random.Next(2) == 0 ? -1 : 1);
        var around = AiMath.Rotate(Vector2.Normalize(offset), turn) * wanted;
        var destination = new Vector3(target.Position.X + around.X, target.Position.Y + around.Y, _host.Position.Z);

        if (arc <= 0f || !_host.MoveTo(destination, false))
        {
            StopGaze();
        }
    }

    private void StopGaze()
    {
        if (!_gazing)
        {
            return;
        }

        _gazing = false;
        _host.SetGaze(false);

        if (_host.IsMoving)
        {
            _host.StopMoving(true);
        }
    }

    private void StartMove()
    {
        if (StartFollowing())
        {
            return;
        }

        if (Target is not { } target)
        {
            ClearTarget();
            ChangeState(AiState.Wait);

            return;
        }

        var distance = Vector3.Distance(_host.Position, target.Position);
        var stop = Math.Max(_settings.TargetDistanceMax, target.Radius + _host.Radius);
        var run = distance > _profile.RunDistance || _host.IsBattlePose;

        if (distance <= stop)
        {
            ChangeState(_host.IsBattlePose ? AiState.SelectAction : AiState.Wait);

            return;
        }

        var toward = Vector3.Normalize(target.Position - _host.Position);
        var wanted = Math.Max(_settings.TargetDistanceMin, target.Radius + _host.Radius);

        if (!_host.MoveTo(target.Position - toward * Math.Min(wanted, distance), run))
        {
            ChangeState(_host.IsBattlePose ? AiState.SelectAction : AiState.Wait);
        }
    }

    private void UpdateMove()
    {
        if (UpdateFollowing() || IsFarFromHome())
        {
            return;
        }

        if (!_host.IsMoving)
        {
            ChangeState(_host.IsBattlePose ? AiState.SelectAction : AiState.Wait);
        }
    }

    private void StartAttackSkill(int group)
    {
        if (_profile.HasSkillGroups && group >= AiProfile.SkillSlots)
        {
            ChangeState(AiState.SelectAction);

            return;
        }

        var slot = ChooseSlot(group);

        if (slot is < 0 or >= AiProfile.SkillSlots || _host.GetSkill(slot) is not { } skill)
        {
            ChangeState(AiState.SelectAction);

            return;
        }

        if (TargetFor(skill) is null)
        {
            ChangeState(AiState.Wait);

            return;
        }

        _skill = skill;
        _skillSlot = slot;
        _skillGroup = group;
        _skillStarted = false;
    }

    private void UpdateAttackSkill(float elapsed)
    {
        var target = _skill is { } chosen ? TargetFor(chosen) : Target;

        if (target is null)
        {
            ClearTarget();
            ChangeState(AiState.Wait);

            return;
        }

        if (_skillStarted)
        {
            if (!_host.IsAttacking)
            {
                var next = ActionAfterSkill(_skillSlot, _skillGroup);

                _skill = null;
                _skillSlot = -1;
                _skillGroup = -1;
                _skillStarted = false;

                ChangeState(next != AiState.None ? next : AiState.SelectAction);
            }

            return;
        }

        if (_skill is null || _stateTime > SkillGiveUpTime || AreMet(_profile.CancelSkillConditions) && _profile.CancelSkillConditions.Count > 0)
        {
            ChangeState(AiState.SelectAction);

            return;
        }

        var distance = SkillDistance(_host.Position, target.Value.Position);

        if (_host.CanAttack && distance >= _skill.RangeMin - RangeSlack && distance <= _skill.RangeMax + target.Value.Radius)
        {
            _host.FaceTo(target.Value.Position);

            if (_host.IsMoving)
            {
                _host.StopMoving(true);
            }

            if (_host.UseSkill(_skill, target.Value))
            {
                StartCooldown(_skill);

                _skillStarted = true;
                _preSkillDamageCount = 0;
                _hitCount = 0;
                _lastSkillTime = 0f;
            }

            return;
        }

        if (_host.IsRooted)
        {
            // A monster that cannot walk gives up on a target that stays out of reach.
            if (_stateTime > RootedGiveUpTime)
            {
                ChangeState(AiState.Wait);
            }

            return;
        }

        if (!_host.CanMove)
        {
            return;
        }

        // Out of reach: goes up to the target, and follows it while it moves.
        if (!_host.IsMoving || _repathTime > RepathPause)
        {
            _repathTime = 0f;

            var toward = Vector3.Normalize(target.Value.Position - _host.Position);
            // A wide monster with a short skill has its nearest stand beyond the skill's own range: the nearest stand wins
            // (a clamp between the two would throw there).
            var nearest = target.Value.Radius + _host.Radius + 10f;
            var farthest = Math.Max(_skill.RangeMax - 10f, 1f);
            var wanted = Math.Max(nearest, Math.Min((_skill.RangeMin + _skill.RangeMax) / 2f, farthest));

            if (!_host.MoveTo(target.Value.Position - toward * Math.Min(wanted, distance), true) && !_host.IsMoving)
            {
                GiveUpReaching();
            }
        }
    }

    /// <summary>
    /// Below this height between the two, the reach of a skill is the distance less that height.
    /// </summary>
    private const float SkillReachLevel = 200f;

    /// <summary>
    /// How far a target is for the reach of a skill: the distance, less the height between the two when they stand on nearly one
    /// level, so a target a little above or below is reached as if it stood level.
    /// </summary>
    private static float SkillDistance(Vector3 from, Vector3 to)
    {
        var distance = Vector3.Distance(from, to);
        var height = MathF.Abs(to.Z - from.Z);

        return height < SkillReachLevel ? distance - height : distance;
    }

    private void StartCommonAction(int index)
    {
        var name = _profile.CommonActions[index];

        _actionEnd = 0f;

        if (string.IsNullOrEmpty(name) || name == "0")
        {
            return;
        }

        _host.StopMoving(true);

        _actionEnd = _host.PlayAction(name);
    }

    private void UpdateCommonAction()
    {
        if (_stateTime >= _actionEnd)
        {
            ChangeState(_host.IsBattlePose ? AiState.SelectAction : AiState.Wait);
        }
    }

    private void StartReturn()
    {
        ClearTarget();

        if (_host.CanMove)
        {
            _host.SetBattlePose(true, false);
            _host.StopMoving(false);
            UpdateReturn();
            _host.SetInvincible(true);
        }
    }

    private void UpdateReturn()
    {
        if (_stateTime > ReturnGiveUpTime)
        {
            _host.Teleport(_host.HomePosition);
            ChangeState(AiState.SelectAction);

            return;
        }

        if (_host.IsMoving || !_host.CanMove)
        {
            return;
        }

        // A way home that is not found now is looked for again until the time is up and the monster is put there.
        if (AiMath.Distance2D(_host.Position, _host.HomePosition) >= _host.Radius + ReturnArrival)
        {
            if (_host.MoveTo(_host.HomePosition, true))
            {
                ClearTarget();
            }

            return;
        }

        // Near its home is home: the monster stays where it stopped and waits.
        ChangeState(AiState.Wait);
    }

    private void StartRunaway()
    {
        if (Target is not { } target || !_host.CanMove)
        {
            return;
        }

        _runawayCount++;

        var destination = PlaceAround(target, _settings.TargetDistanceMin, Math.Max(_settings.TargetDistanceMax, _settings.TargetDistanceMin), 180f - _settings.AngleMax, 180f);

        _host.MoveTo(destination, true);
    }
}
