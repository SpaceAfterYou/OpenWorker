namespace OpenWorker.Hotspot;

/// <summary>
/// One action waiting in the queue of <see cref="SimulationClock"/>. The clock reuses these, so a handle names one by
/// its version as well: a handle of an action that has run or been cancelled no longer reaches the next one.
/// </summary>
internal sealed class Scheduled
{
    public Action? Action;
    public ScheduleGroup? Group;
    public int Version;
}

/// <summary>
/// What <see cref="SimulationClock.Schedule"/> gives back: the way to call the action off before it runs. The default
/// value is an action that was never scheduled.
/// </summary>
public readonly struct ScheduledAction
{
    private readonly SimulationClock? _clock;
    private readonly Scheduled? _item;
    private readonly int _version;

    internal ScheduledAction(SimulationClock clock, Scheduled item)
    {
        _clock = clock;
        _item = item;
        _version = item.Version;
    }

    /// <summary>
    /// The action has neither run nor been cancelled.
    /// </summary>
    public bool IsPending => _item is { Action: not null } item && item.Version == _version;

    /// <summary>
    /// Calls the action off. True if it was still waiting; nothing happens to an action that has run already.
    /// </summary>
    public bool Cancel()
    {
        return IsPending && _clock!.Cancel(_item!);
    }
}

/// <summary>
/// The actions one owner (a maze) has put on the clock, so that the owner can call them all off when it ends.
/// A group that is closed takes no more: whatever asks for an action after the end is ignored.
/// </summary>
public sealed class ScheduleGroup
{
    private readonly SimulationClock _clock;
    private readonly HashSet<Scheduled> _waiting = new(ReferenceEqualityComparer.Instance);

    internal ScheduleGroup(SimulationClock clock)
    {
        _clock = clock;
    }

    /// <summary>
    /// The actions of the group that wait to run.
    /// </summary>
    public int Count => _waiting.Count;

    public bool IsClosed { get; private set; }

    /// <summary>
    /// Runs <paramref name="action"/> on the world loop at the first tick at least <paramref name="delay"/> of game time
    /// from now, unless the group is closed first. A closed group schedules nothing and gives the default handle.
    /// </summary>
    public ScheduledAction Schedule(TimeSpan delay, Action action)
    {
        return IsClosed ? default : _clock.Schedule(delay, action, this);
    }

    /// <summary>
    /// Runs <paramref name="action"/> at the first tick whose game time (<see cref="SimulationClock.Now"/>) reaches
    /// <paramref name="due"/>, a moment read from <see cref="SimulationClock.Time"/>; at the next tick when it is past.
    /// </summary>
    public ScheduledAction ScheduleAt(TimeSpan due, Action action)
    {
        return IsClosed ? default : _clock.ScheduleAt(due, action, this);
    }

    /// <summary>
    /// Calls off every action of the group that has not run, and takes no more.
    /// </summary>
    public void Close()
    {
        IsClosed = true;

        CancelAll();
    }

    /// <summary>
    /// Calls off every action of the group that has not run. The group goes on taking new ones.
    /// </summary>
    public void CancelAll()
    {
        foreach (var item in _waiting)
        {
            _clock.Forget(item);
        }

        _waiting.Clear();
    }

    internal void Add(Scheduled item)
    {
        _waiting.Add(item);
    }

    internal void Remove(Scheduled item)
    {
        _waiting.Remove(item);
    }
}
