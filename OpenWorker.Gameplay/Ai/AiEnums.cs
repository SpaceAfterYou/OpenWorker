namespace OpenWorker.Gameplay.Ai;

/// <summary>
/// What a monster is busy with. The numbers are the ones the scripts of the monsters use.
/// </summary>
public enum AiState
{
    None = 0,
    Wait = 1,
    Patrol = 2,
    SelectAction = 3,
    BattleWait = 4,
    BattleMove = 5,
    Gaze = 6,
    Move = 7,
    Attack = 8,
    AttackSkill1 = 9,
    AttackSkill2 = 10,
    AttackSkill3 = 11,
    AttackSkill4 = 12,
    AttackSkill5 = 13,
    AttackSkill6 = 14,
    AttackSkill7 = 15,
    AttackSkill8 = 16,
    AttackSkill9 = 17,
    AttackSkill10 = 18,
    CommonAction1 = 19,
    CommonAction2 = 20,
    CommonAction3 = 21,
    CommonAction4 = 22,
    CommonAction5 = 23,
    CommonAction6 = 24,
    CommonAction7 = 25,
    CommonAction8 = 26,
    CommonAction9 = 27,
    CommonAction10 = 28,
    Return = 29,
    Suicide = 30,
    Recovery = 31,
    TraceMove = 32,
    RequestHelp = 33,
    RequestSupport = 34,
    Runaway = 35,
    BeforeState = 36,
    SwitchState1 = 37,
    SwitchState2 = 38,
    Protection = 39,
    ProtectMove = 40,
    ProtectGaze = 41,
    ProtectWait = 42,
    Max = 43
}

/// <summary>
/// What a condition of a transition looks at.
/// </summary>
public enum AiVariable
{
    None = 0,
    IsTarget = 1,
    TargetDistance = 2,
    TargetDistanceCapsule = 3,
    TargetNumber = 4,
    TargetDirection = 5,
    CreationPositionDistance = 6,
    Timer = 7,
    StateTime = 8,
    IsAttack = 9,
    Health = 10,
    IsHealthChanged = 11,
    Random = 12,
    RandomPreviousValue = 13,
    IsMoving = 14,
    TargetHealth = 15,
    RequestHelpNumber = 16,
    PreviousState = 17,
    NearTargetDistance = 18,
    TargetListNumber = 19,
    MonsterCount = 20,
    SpawnMonsterCount = 21,
    SectorMonsterCount = 22,
    SectorMonsterCountById = 23,
    PreSkillDamageCount = 24,
    TargetSkill = 25,
    TargetState = 26,
    TargetDamageState = 27,
    TargetMoveState = 28,
    TargetBuffIndex = 29,
    TargetAttacker = 30,
    TargetCombo = 31,
    TargetLook = 32,
    PatrolState = 33,
    LastSkillTime = 34,
    LastDamageTime = 35,
    SpawnTime = 36,
    ActivateTime = 37,
    GuardDistance = 38,
    GuardDistanceCapsule = 39,
    GuardHealth = 40,
    GuardFriendCount = 41,
    GuardEnemyCount = 42,
    CanMoveTime = 43,
    CanAttackTime = 44,
    GlobalCooldown = 45,
    GroupCooldown = 46,
    SkillCooldown = 47,
    Fuzzy = 48,
    StateEndTime = 49,
    StateFailCount = 50,
    FriendCount = 51,
    EnemyCount = 52,
    AggroCount = 53,
    HitCount = 54,
    MoveDistanceAfterSkill = 55,
    UserCount = 56,
    Max = 57
}

/// <summary>
/// The numbers a state keeps for itself.
/// </summary>
public enum AiStateVariable
{
    /// <summary>
    /// How long the state lasts, in milliseconds: one value or a range.
    /// </summary>
    LifeTime = 0,
    MoveDistance = 1,
    TargetDistance = 2,
    Angle = 3,

    /// <summary>
    /// Where the state goes when its time is over: the state and its weight.
    /// </summary>
    NextState = 4,
    Max = 5
}

public enum AiOperator
{
    None = 0,
    Equal = 1,
    NotEqual = 2,
    Greater = 3,
    Less = 4,
    BitAnd = 5,
    InRange = 6,
    NotInRange = 7,
    RangeEqual = 8
}
