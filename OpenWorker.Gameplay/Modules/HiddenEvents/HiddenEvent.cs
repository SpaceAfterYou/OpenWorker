namespace OpenWorker.Gameplay.Modules.HiddenEvents;

/// <summary>
/// How a condition of a hidden event counts (the kind of a row of <c>tb_Event_Condition</c>).
/// </summary>
public enum HiddenEventKind : byte
{
    /// <summary>Monsters killed: the monster of the row, or any.</summary>
    Kill = 1,

    /// <summary>A number reached: the longest combo is kept at its highest, the rest add up.</summary>
    Reach = 2,

    /// <summary>An amount collected.</summary>
    Collect = 3,

    /// <summary>Uses: met at the count.</summary>
    Use = 4,

    /// <summary>At most so many uses: failed past the count, met at the clear otherwise.</summary>
    UseLess = 5
}

/// <summary>
/// What a condition of a hidden event counts (the target of a row of <c>tb_Event_Condition</c>).
/// </summary>
public enum HiddenEventTarget : byte
{
    Monster = 1,
    Jump = 2,
    Stamina = 3,
    Gold = 4,
    Ether = 5,
    Soul = 6,
    MaxCombo = 7,

    /// <summary>A skill of the kind the table numbers 5: the last skill of every hero.</summary>
    ErosionSkill = 8,

    /// <summary>A skill of the kind the table numbers 4: the skill before it.</summary>
    PeakSkill = 9,
    ItemUsed = 10,
    ArmorBreak = 11,
    SkillLink = 12,
    JumpAttack = 13,
    Evade = 14,
    MonsterHitInAir = 15,
    JumpAttackHit = 16
}

/// <summary>
/// Where a hidden event stands, as the client is told (11/37).
/// </summary>
public enum HiddenEventState : byte
{
    None = 0,
    Started = 1,
    Progress = 2,
    Met = 3,
    Failed = 4
}

/// <summary>
/// The condition a hidden event asks for.
/// </summary>
public readonly record struct HiddenEventCondition(int Id, HiddenEventKind Kind, HiddenEventTarget Target, int TargetId, int Count);

/// <summary>
/// The item a met hidden event gives each player of the maze.
/// </summary>
public readonly record struct HiddenEventReward(int Item, short Count);

/// <summary>
/// The hidden event of one maze: the condition picked for it, what has been counted, and whether it was met. The maze tells
/// its players every change.
/// </summary>
public sealed class HiddenEvent(ushort id, HiddenEventCondition condition, HiddenEventReward reward, Func<int, bool> countsAnyItem)
{
    public ushort Id { get; } = id;

    public HiddenEventCondition Condition { get; } = condition;

    public HiddenEventState State { get; private set; } = HiddenEventState.Started;

    public int Counted { get; private set; }

    /// <summary>
    /// The item of the event once it has been met, else nothing.
    /// </summary>
    public HiddenEventReward? Reward => State == HiddenEventState.Met && reward.Item > 0 && reward.Count > 0 ? reward : null;

    /// <summary>
    /// The players have been told that the event started: from now on it counts.
    /// </summary>
    public void Begin()
    {
        if (State == HiddenEventState.Started)
        {
            State = HiddenEventState.Progress;
        }
    }

    /// <summary>
    /// Something the condition may count has happened: <paramref name="target"/> with its monster or item and an amount. True when
    /// the count or the state changed.
    /// </summary>
    public bool Count(HiddenEventTarget target, int what, int amount)
    {
        if (State != HiddenEventState.Progress || target != Condition.Target || amount <= 0)
        {
            return false;
        }

        switch (Condition.Kind)
        {
            case HiddenEventKind.Kill:
                if (Condition.TargetId != 0 && Condition.TargetId != what)
                {
                    return false;
                }

                Add(amount);
                break;

            case HiddenEventKind.Reach when target == HiddenEventTarget.MaxCombo:
                if (amount <= Counted)
                {
                    return false;
                }

                Counted = amount;
                break;

            case HiddenEventKind.Reach:
                Add(amount);
                break;

            case HiddenEventKind.Collect or HiddenEventKind.Use or HiddenEventKind.UseLess:
                if (target == HiddenEventTarget.ItemUsed && !IsItemCounted(what))
                {
                    return false;
                }

                Add(amount);

                if (Condition.Kind == HiddenEventKind.UseLess)
                {
                    if (Counted > Condition.Count)
                    {
                        State = HiddenEventState.Failed;
                    }

                    return true;
                }

                break;

            default:
                return false;
        }

        if (Counted >= Condition.Count)
        {
            State = HiddenEventState.Met;
        }

        return true;
    }

    /// <summary>
    /// The maze is cleared: an event still going is met when it only asked to keep under a count, and failed otherwise. True
    /// when the state changed.
    /// </summary>
    public bool Finish()
    {
        if (State != HiddenEventState.Progress)
        {
            return false;
        }

        State = Condition.Kind == HiddenEventKind.UseLess ? HiddenEventState.Met : HiddenEventState.Failed;

        return true;
    }

    private void Add(int amount)
    {
        Counted = (int)Math.Min((long)Counted + amount, int.MaxValue);
    }

    // An item of the row counts only itself; with none, any item used for its effect counts.
    private bool IsItemCounted(int item)
    {
        return Condition.TargetId != 0 ? Condition.TargetId == item : countsAnyItem(item);
    }
}
