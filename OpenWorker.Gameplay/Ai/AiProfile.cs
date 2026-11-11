namespace OpenWorker.Gameplay.Ai;

/// <summary>
/// One test in a transition: a number the monster knows about against what the script asks.
/// </summary>
/// <param name="Low">The number to compare with, or the lower end of a range.</param>
/// <param name="High">The upper end of a range. For the operators that compare with one number it is the other end
/// of the numbers the monster chooses from, when <paramref name="IsRandom"/> is set.</param>
public sealed record AiCondition(AiVariable Variable, AiOperator Operator, double Low, double High, bool IsRandom)
{
    /// <summary>
    /// The variables that read the second number of the script as an argument and not as a range.
    /// </summary>
    public static bool TakesArgument(AiVariable variable)
    {
        return variable is AiVariable.TargetBuffIndex or AiVariable.StateFailCount or AiVariable.FriendCount
            or AiVariable.EnemyCount or AiVariable.UserCount or AiVariable.GuardFriendCount or AiVariable.GuardEnemyCount
            or AiVariable.GroupCooldown or AiVariable.SkillCooldown or AiVariable.StateEndTime;
    }

    /// <summary>
    /// The timers of a transition are read by the machine itself.
    /// </summary>
    public static bool IsTimer(AiVariable variable)
    {
        return variable is AiVariable.Timer or AiVariable.CanMoveTime or AiVariable.CanAttackTime;
    }

    /// <summary>
    /// Whether a variable is compared as a whole number. The others are real numbers, compared as they are.
    /// </summary>
    public static bool IsInteger(AiVariable variable)
    {
        return variable switch
        {
            AiVariable.TargetDistance or AiVariable.TargetDistanceCapsule or AiVariable.TargetDirection
                or AiVariable.CreationPositionDistance or AiVariable.StateTime or AiVariable.TargetLook
                or AiVariable.LastSkillTime or AiVariable.LastDamageTime or AiVariable.SpawnTime
                or AiVariable.ActivateTime or AiVariable.GuardDistance or AiVariable.GuardDistanceCapsule
                or AiVariable.Fuzzy or AiVariable.StateEndTime or AiVariable.MoveDistanceAfterSkill
                or AiVariable.Timer or AiVariable.CanMoveTime or AiVariable.CanAttackTime
                or AiVariable.PreviousState or AiVariable.NearTargetDistance or AiVariable.TargetListNumber => false,
            _ => true
        };
    }

    /// <summary>
    /// Tests a value that was read for this condition against the number it is to be compared with.
    /// </summary>
    public bool IsMet(double value, double low)
    {
        if (IsInteger(Variable))
        {
            var number = (int)value;
            var first = (int)low;
            var second = (int)High;

            return Operator switch
            {
                AiOperator.Equal or AiOperator.RangeEqual => number == first,
                AiOperator.NotEqual => number != first,
                AiOperator.Greater => number > first,
                AiOperator.Less => number < first,
                AiOperator.BitAnd => (first & number) != 0,
                AiOperator.InRange => first <= number && number < second,
                AiOperator.NotInRange => first > number || number >= second,
                _ => false
            };
        }

        var single = (float)value;
        var begin = (float)low;
        var end = (float)High;

        return Operator switch
        {
            AiOperator.Equal or AiOperator.RangeEqual => single == begin,
            AiOperator.NotEqual => single != begin,
            AiOperator.Greater => single > begin,
            AiOperator.Less => begin > single,
            AiOperator.InRange => single >= begin && end > single,
            AiOperator.NotInRange => begin > single || single >= end,
            _ => false
        };
    }
}

/// <summary>
/// A way out of a state: the new state if every condition holds.
/// </summary>
public sealed record AiTransition(AiState Output, IReadOnlyList<AiCondition> Conditions);

/// <summary>
/// What a state keeps for itself, from the script of the monster.
/// </summary>
public sealed record AiStateSettings
{
    public const int NoTime = -1;

    /// <summary>
    /// The state lasts this long, in milliseconds, or a time between the two. A negative value means it does not end by itself.
    /// </summary>
    public int LifeMin { get; init; } = NoTime;

    public int LifeMax { get; init; }
    public float MoveDistanceMin { get; init; }
    public float MoveDistanceMax { get; init; }
    public float TargetDistanceMin { get; init; }
    public float TargetDistanceMax { get; init; }
    public float AngleMin { get; init; }
    public float AngleMax { get; init; }
    public IReadOnlyList<(AiState State, int Weight)> NextStates { get; init; } = [];

    /// <summary>
    /// Where the state goes when its time is over and none of the weights is more than zero: the last state the script named.
    /// </summary>
    public AiState DefaultNext { get; init; }
}

/// <summary>
/// The conditions that choose a skill or decide what follows it.
/// </summary>
public sealed record AiSkillRule(IReadOnlyList<AiCondition> Conditions, AiState NextState = AiState.None);

/// <summary>
/// How a monster thinks, as its script describes it. Many monsters share one profile and none of them changes it.
/// </summary>
public sealed class AiProfile
{
    public const int SkillSlots = 10;
    public const int ReservedConditions = 20;

    public static AiProfile Empty { get; } = new AiProfileBuilder().Build();

    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// How often the monster thinks, in seconds.
    /// </summary>
    public float CheckTime { get; init; } = 1f;

    /// <summary>
    /// Looks for somebody to attack without being asked to.
    /// </summary>
    public bool SeeksEnemies { get; init; }

    public float SearchDelay { get; init; } = 0.1f;
    public float SightDistance { get; init; }
    public float TargetLostDistance { get; init; }
    public float ReturnDistance { get; init; } = 20000f;
    public int ReturnHealth { get; init; }
    public float RunDistance { get; init; } = 700f;
    public int DamageAggressive { get; init; } = 10000;
    public float AggroCheckTime { get; init; }
    public float DamageAggroResetTime { get; init; } = 10f;
    public float DamageAggroResetDistance { get; init; }
    public float SpawnAggroDistance { get; init; }
    public float SpawnAggroValue { get; init; }
    public bool ClearsTarget { get; init; }

    /// <summary>
    /// The skill the monster plays when its super armor is whole again, or zero for none.
    /// </summary>
    public int SuperArmorSkill { get; init; }

    public float SuicideTime { get; init; }
    public string DeathAction { get; init; } = string.Empty;
    public int RunawayHealth { get; init; }
    public int RunawayCount { get; init; }
    public float RunawayMinTime { get; init; }
    public float RunawayMaxTime { get; init; }

    /// <summary>
    /// What a helper that fights beside a person keeps for itself: how far from its owner it may be before it comes back
    /// to it, the skill it plays while it gets up, and how it gets away from blows.
    /// </summary>
    public AiFollowerSettings Follower { get; init; } = new();

    /// <summary>
    /// The states the monster has: it cannot enter any other.
    /// </summary>
    public IReadOnlySet<AiState> States { get; init; } = new HashSet<AiState>();

    public IReadOnlyDictionary<AiState, AiStateSettings> Settings { get; init; } = new Dictionary<AiState, AiStateSettings>();
    public IReadOnlyDictionary<AiState, IReadOnlyList<AiTransition>> Transitions { get; init; } = new Dictionary<AiState, IReadOnlyList<AiTransition>>();

    /// <summary>
    /// The animations of the common actions, indexed from zero.
    /// </summary>
    public IReadOnlyList<string> CommonActions { get; init; } = new string[SkillSlots];

    public bool HasSkillGroups { get; init; }
    public IReadOnlyList<int> SkillSortTypes { get; init; } = new int[SkillSlots];

    /// <summary>
    /// For each group of skills the slots of its three skills counted from one, zero for none.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<int>> SkillGroups { get; init; } = Enumerable.Range(0, SkillSlots).Select(_ => (IReadOnlyList<int>)new int[3]).ToArray();

    public IReadOnlyList<int> SkillGroupRatios { get; init; } = new int[SkillSlots];

    public IReadOnlyList<AiSkillRule> SkillRules { get; init; } = Enumerable.Range(0, SkillSlots).Select(_ => new AiSkillRule([])).ToArray();

    /// <summary>
    /// By group and then by slot.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<AiSkillRule>> SkillGroupRules { get; init; } = Grid();

    public IReadOnlyList<IReadOnlyList<AiSkillRule>> ActionsAfterSkill { get; init; } = Grid();
    public IReadOnlyList<AiCondition> CancelSkillConditions { get; init; } = [];

    internal static IReadOnlyList<IReadOnlyList<AiSkillRule>> Grid()
    {
        return Enumerable.Range(0, SkillSlots)
            .Select(_ => (IReadOnlyList<AiSkillRule>)Enumerable.Range(0, SkillSlots).Select(_ => new AiSkillRule([])).ToArray())
            .ToArray();
    }

    public AiStateSettings GetSettings(AiState state)
    {
        return Settings.TryGetValue(state, out var settings) ? settings : new AiStateSettings();
    }

    public IReadOnlyList<AiTransition> GetTransitions(AiState state)
    {
        return Transitions.TryGetValue(state, out var transitions) ? transitions : [];
    }
}

/// <summary>
/// Collects what a script says and makes a profile of it. The calls follow the ones the scripts make.
/// </summary>
public sealed class AiProfileBuilder
{
    private const int WaitLifeMin = 4000;
    private const int WaitLifeMax = 5000;

    /// <summary>
    /// The states every monster has, and where each of them goes when it is over. A script adds to this.
    /// </summary>
    private static readonly (AiState State, AiState Next)[] Defaults =
    [
        (AiState.Patrol, AiState.Wait),
        (AiState.BattleWait, AiState.SelectAction),
        (AiState.BattleMove, AiState.SelectAction),
        (AiState.Gaze, AiState.SelectAction),
        (AiState.Move, AiState.SelectAction),
        (AiState.Attack, AiState.AttackSkill1),
        (AiState.AttackSkill1, AiState.SelectAction),
        (AiState.AttackSkill2, AiState.SelectAction),
        (AiState.AttackSkill3, AiState.SelectAction),
        (AiState.AttackSkill4, AiState.SelectAction),
        (AiState.AttackSkill5, AiState.SelectAction),
        (AiState.AttackSkill6, AiState.SelectAction),
        (AiState.AttackSkill7, AiState.SelectAction),
        (AiState.AttackSkill8, AiState.SelectAction),
        (AiState.AttackSkill9, AiState.SelectAction),
        (AiState.AttackSkill10, AiState.SelectAction),
        (AiState.RequestHelp, AiState.SelectAction),
        (AiState.RequestSupport, AiState.SelectAction),
        (AiState.Runaway, AiState.SelectAction),
        (AiState.TraceMove, AiState.SelectAction),
        (AiState.Return, AiState.Wait),
        (AiState.ProtectGaze, AiState.Protection),
        (AiState.ProtectMove, AiState.Protection),
        (AiState.ProtectWait, AiState.Protection)
    ];

    private static readonly AiState[] DefaultStates =
    [
        AiState.Wait, AiState.Patrol, AiState.SelectAction, AiState.BattleWait, AiState.Move, AiState.BattleMove,
        AiState.Gaze, AiState.AttackSkill1, AiState.Return, AiState.Runaway, AiState.Suicide, AiState.Recovery,
        AiState.TraceMove, AiState.Protection, AiState.ProtectMove, AiState.ProtectGaze, AiState.ProtectWait
    ];

    private readonly HashSet<AiState> _states = [.. DefaultStates];
    private readonly Dictionary<AiState, AiStateSettings> _settings = Defaults.ToDictionary(e => e.State, e => new AiStateSettings { NextStates = [(e.Next, 0)], DefaultNext = e.Next });
    private readonly Dictionary<AiState, List<AiTransition>> _transitions = [];
    private readonly AiCondition?[] _reserved = new AiCondition?[AiProfile.ReservedConditions];
    private readonly string[] _commonActions = Enumerable.Repeat(string.Empty, AiProfile.SkillSlots).ToArray();
    private readonly int[] _sortTypes = new int[AiProfile.SkillSlots];
    private readonly int[][] _groups = Enumerable.Range(0, AiProfile.SkillSlots).Select(_ => new int[3]).ToArray();
    private readonly int[] _ratios = new int[AiProfile.SkillSlots];
    private readonly List<AiCondition>[] _skillConditions = Enumerable.Range(0, AiProfile.SkillSlots).Select(_ => new List<AiCondition>()).ToArray();
    private readonly List<AiCondition>[][] _groupConditions = Table();
    private readonly List<AiCondition>[][] _afterConditions = Table();
    private readonly AiState[][] _afterStates = Enumerable.Range(0, AiProfile.SkillSlots).Select(_ => new AiState[AiProfile.SkillSlots]).ToArray();
    private readonly List<AiCondition> _cancel = [];

    private string _name = string.Empty;
    private float _checkTime = 1f;
    private bool _seeks;
    private float _searchDelay = 0.1f;
    private float _sight;
    private float _lost;
    private float _returnDistance = 20000f;
    private int _returnHealth;
    private float _runDistance = 700f;
    private int _damageAggressive = 10000;
    private float _aggroCheck;
    private float _aggroResetTime = 10f;
    private float _aggroResetDistance;
    private float _spawnAggroDistance;
    private float _spawnAggroValue;
    private bool _clearsTarget;
    private int _superArmorSkill;
    private float _suicide;
    private string _deathAction = string.Empty;
    private int _runawayHealth;
    private int _runawayCount;
    private float _runawayMin;
    private float _runawayMax;
    private bool _hasGroups;
    private AiFollowerSettings _follower = new();

    private static List<AiCondition>[][] Table()
    {
        return Enumerable.Range(0, AiProfile.SkillSlots)
            .Select(_ => Enumerable.Range(0, AiProfile.SkillSlots).Select(_ => new List<AiCondition>()).ToArray())
            .ToArray();
    }

    public AiProfileBuilder Named(string name)
    {
        _name = name;

        return this;
    }

    public void SetAiCheckTime(float seconds) => _checkTime = seconds;

    public void SetFindEnemyInfo(bool seeks, float delay, float distance)
    {
        _seeks = seeks;
        _searchDelay = delay;
        _sight = Math.Max(_sight, distance);
    }

    public void SetFirstAttacker(bool seeks, float delay)
    {
        _seeks = seeks;
        _searchDelay = delay;
    }

    public void SetTakeTargetInfo(float distance) => _sight = Math.Max(_sight, distance);

    public void SetTargetLostInfo(float distance) => _lost = distance;

    public void SetReturnDistance(float distance, int health = 0)
    {
        _returnDistance = distance;
        _returnHealth = health;
    }

    public void SetRunDistance(float distance) => _runDistance = distance;

    public void SetDmgAggressive(int chance, float checkTime)
    {
        _damageAggressive = chance;
        _aggroCheck = checkTime;
    }

    public void SetDmgAggroReset(float seconds, float distance)
    {
        _aggroResetTime = seconds;
        _aggroResetDistance = distance;
    }

    public void SetSpawnAggro(float distance, float value)
    {
        _spawnAggroDistance = distance;
        _spawnAggroValue = value;
    }

    public void SetEnableClearTarget(bool enable) => _clearsTarget = enable;

    /// <summary>
    /// The skill the monster plays each time its super armor is whole again.
    /// </summary>
    public void SetSuperArmorSkill(int skill) => _superArmorSkill = skill;

    public void SetSuicideTime(float seconds) => _suicide = seconds;

    public void SetDeathAction(string animation) => _deathAction = animation;

    public void SetRunawayInfo(int health, int count, float minTime, float maxTime)
    {
        _runawayHealth = health;
        _runawayCount = count;
        _runawayMin = minTime;
        _runawayMax = maxTime;
    }

    /// <summary>
    /// How far from its owner a helper may be, out of a fight and in one, before it comes back to the owner at once.
    /// </summary>
    public void SetHelperFarDist(float distance, float battleDistance)
    {
        _follower = _follower with { FarDistance = distance, FarBattleDistance = battleDistance };
    }

    /// <summary>
    /// The skill a helper plays when it is beaten down and gets up again.
    /// </summary>
    public void SetRecoverySkill(int skill) => _follower = _follower with { RecoverySkill = skill };

    /// <summary>
    /// How the monster gets away from a run of blows: the points the blows add up to, the chance in ten thousand, the
    /// distances, the time after which the points start again, the kind of escape and its value (a skill for kind 2).
    /// </summary>
    public void SetEscapeInfo(float points, int chance, float minDistance, float maxDistance, float resetTime, byte kind, int value)
    {
        _follower = _follower with { Escape = new AiEscape(points, chance, minDistance, maxDistance, resetTime, kind, value) };
    }

    public void SetCommonAction(int index, string animation)
    {
        if (index is >= 0 and < AiProfile.SkillSlots)
        {
            _commonActions[index] = animation;
        }
    }

    public void SetSkillGroupInfo(int group, int sortType, int first, int second, int third)
    {
        if (group is < 0 or >= AiProfile.SkillSlots)
        {
            return;
        }

        _hasGroups = true;
        _sortTypes[group] = sortType;
        _groups[group][0] = first;
        _groups[group][1] = second;
        _groups[group][2] = third;
    }

    public void SetSkillGroupRate(params int[] ratios)
    {
        _hasGroups = true;

        for (var i = 0; i < AiProfile.SkillSlots; i++)
        {
            _ratios[i] = i < ratios.Length ? ratios[i] : 0;
        }
    }

    public void RegisterStateFunctions(AiState state)
    {
        if (state is > AiState.None and < AiState.Max)
        {
            _states.Add(state);
        }
    }

    public void RegisterStateVars(AiState state, AiStateVariable variable, int first, int second)
    {
        if (state is < AiState.None or >= AiState.Max || variable is < 0 or >= AiStateVariable.Max)
        {
            return;
        }

        // A monster waits four to five seconds whatever its script says.
        if (state == AiState.Wait && variable == AiStateVariable.LifeTime)
        {
            first = WaitLifeMin;
            second = WaitLifeMax;
        }

        var settings = _settings.GetValueOrDefault(state) ?? new AiStateSettings();

        _settings[state] = variable switch
        {
            AiStateVariable.LifeTime => settings with { LifeMin = first, LifeMax = second },
            AiStateVariable.MoveDistance => settings with { MoveDistanceMin = first, MoveDistanceMax = second },
            AiStateVariable.TargetDistance => settings with { TargetDistanceMin = first, TargetDistanceMax = second },
            AiStateVariable.Angle => settings with { AngleMin = first, AngleMax = second },
            _ => settings with { NextStates = [.. settings.NextStates.Where(e => e.State != (AiState)first), ((AiState)first, second)], DefaultNext = (AiState)first }
        };
    }

    public void RegisterConditions(AiState state, AiState output, AiVariable variable, string op, double first, double second = 0, int transition = 1)
    {
        if (state is <= AiState.None or >= AiState.Max || variable is < 0 or >= AiVariable.Max || Create(variable, op, first, second) is not { } condition)
        {
            return;
        }

        _states.Add(state);

        if (!_transitions.TryGetValue(state, out var list))
        {
            list = [];
            _transitions.Add(state, list);
        }

        // The same exit can be reached in several ways: the number says which of them the condition belongs to.
        var skip = Math.Max(0, transition - 1);
        var index = -1;

        for (var i = 0; i < list.Count; i++)
        {
            if (list[i].Output == output && skip-- == 0)
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            list.Add(new AiTransition(output, [condition]));
        }
        else
        {
            list[index] = list[index] with { Conditions = [.. list[index].Conditions, condition] };
        }
    }

    public void SetReservedCondition(int index, AiVariable variable, string op, double first, double second = 0)
    {
        if (index is >= 0 and < AiProfile.ReservedConditions && variable is >= 0 and < AiVariable.Max)
        {
            _reserved[index] = Create(variable, op, first, second);
        }
    }

    public void CombineReservedConditions(AiState state, AiState output, params int[] indexes)
    {
        if (state is <= AiState.None or >= AiState.Max)
        {
            return;
        }

        var conditions = indexes
            .Where(e => e is >= 0 and < AiProfile.ReservedConditions)
            .Select(e => _reserved[e])
            .OfType<AiCondition>()
            .ToArray();

        _states.Add(state);

        if (!_transitions.TryGetValue(state, out var list))
        {
            list = [];
            _transitions.Add(state, list);
        }

        list.Add(new AiTransition(output, conditions));
    }

    public void RegisterSkillConditions(int slot, AiVariable variable, string op, double first, double second = 0, int group = -1)
    {
        if (slot < 1 || slot > AiProfile.SkillSlots || variable is < 0 or >= AiVariable.Max || Create(variable, op, first, second) is not { } condition)
        {
            return;
        }

        if (group == -1)
        {
            _skillConditions[slot - 1].Add(condition);
        }
        else if (group is >= 0 and < AiProfile.SkillSlots)
        {
            _groupConditions[group][slot - 1].Add(condition);
        }
    }

    public void RegisterActionAfterSkill(int slot, AiState next, AiVariable variable, string op, double first, double second = 0, int group = 0)
    {
        if (slot < 1 || slot > AiProfile.SkillSlots || group is < 0 or >= AiProfile.SkillSlots
            || variable is < 0 or >= AiVariable.Max || Create(variable, op, first, second) is not { } condition)
        {
            return;
        }

        _afterConditions[group][slot - 1].Add(condition);
        _afterStates[group][slot - 1] = next;
    }

    public void RegisterCancelSkillConditions(AiVariable variable, string op, double first, double second = 0)
    {
        if (variable is >= 0 and < AiVariable.Max && Create(variable, op, first, second) is { } condition)
        {
            _cancel.Add(condition);
        }
    }

    public AiProfile Build()
    {
        return new AiProfile
        {
            Name = _name,
            CheckTime = _checkTime,
            SeeksEnemies = _seeks,
            SearchDelay = _searchDelay,
            SightDistance = _sight,
            TargetLostDistance = _lost,
            ReturnDistance = _returnDistance,
            ReturnHealth = _returnHealth,
            RunDistance = _runDistance,
            DamageAggressive = _damageAggressive,
            AggroCheckTime = _aggroCheck,
            DamageAggroResetTime = _aggroResetTime,
            DamageAggroResetDistance = _aggroResetDistance,
            SpawnAggroDistance = _spawnAggroDistance,
            SpawnAggroValue = _spawnAggroValue,
            ClearsTarget = _clearsTarget,
            SuperArmorSkill = _superArmorSkill,
            SuicideTime = _suicide,
            DeathAction = _deathAction,
            RunawayHealth = _runawayHealth,
            RunawayCount = _runawayCount,
            RunawayMinTime = _runawayMin,
            RunawayMaxTime = _runawayMax,
            Follower = _follower,
            States = new HashSet<AiState>(_states),
            Settings = new Dictionary<AiState, AiStateSettings>(_settings),
            Transitions = _transitions.ToDictionary(e => e.Key, e => (IReadOnlyList<AiTransition>)e.Value.ToArray()),
            CommonActions = [.. _commonActions],
            HasSkillGroups = _hasGroups,
            SkillSortTypes = [.. _sortTypes],
            SkillGroups = _groups.Select(e => (IReadOnlyList<int>)[.. e]).ToArray(),
            SkillGroupRatios = [.. _ratios],
            SkillRules = _skillConditions.Select(e => new AiSkillRule([.. e])).ToArray(),
            SkillGroupRules = _groupConditions.Select(row => (IReadOnlyList<AiSkillRule>)row.Select(e => new AiSkillRule([.. e])).ToArray()).ToArray(),
            ActionsAfterSkill = _afterConditions
                .Select((row, group) => (IReadOnlyList<AiSkillRule>)row.Select((e, slot) => new AiSkillRule([.. e], _afterStates[group][slot])).ToArray())
                .ToArray(),
            CancelSkillConditions = [.. _cancel]
        };
    }

    private static AiCondition? Create(AiVariable variable, string op, double first, double second)
    {
        var kind = op switch
        {
            "=" => AiOperator.Equal,
            "!=" => AiOperator.NotEqual,
            ">" => AiOperator.Greater,
            "<" => AiOperator.Less,
            "&" => AiOperator.BitAnd,
            "<>" => AiOperator.InRange,
            "!<>" => AiOperator.NotInRange,
            "&=" => AiOperator.RangeEqual,
            _ => AiOperator.None
        };

        if (kind == AiOperator.None)
        {
            return null;
        }

        // Only a plain comparison with a second number picks the number from a range, and only for what does not read the second one itself.
        var random = kind < AiOperator.InRange && second != 0 && !AiCondition.TakesArgument(variable) && !AiCondition.IsTimer(variable);

        return new AiCondition(variable, kind, first, second, random);
    }
}
