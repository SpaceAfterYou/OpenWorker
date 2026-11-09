using OpenWorker.Hotspot.Modules.Persons.Types;

namespace OpenWorker.Gameplay.Modules.Progress;

/// <summary>
/// The points of a district and whether its reward was taken.
/// </summary>
public sealed class DistrictProgress
{
    public int District { get; set; }

    public int Point { get; set; }

    public bool Rewarded { get; set; }

    public DistrictStateValue ToValue() => new() { District = District, Point = Point, Reward = Rewarded };
}

/// <summary>
/// The points of a group of mazes: the maze cleared last, the district of the group, how many clears, the points and how
/// many steps of the reward were taken.
/// </summary>
public sealed class MazeGroupProgress
{
    public int Group { get; set; }

    public int Maze { get; set; }

    public int District { get; set; }

    public int Clears { get; set; }

    public int Point { get; set; }

    public byte Step { get; set; }

    public MazeStateValue ToValue() => new()
    {
        Maze = Maze,
        Group = Group,
        District = District,
        ClearCount = Clears,
        SharePoint = Point,
        RewardStep = Step
    };
}

/// <summary>
/// The clears of one maze by the person: how many, the best rank and the fastest time.
/// </summary>
public sealed class MazeRecord
{
    public int Maze { get; set; }

    public int Clears { get; set; }

    /// <summary>From 0 (F) to 5 (SS).</summary>
    public byte BestRank { get; set; }

    /// <summary>The fastest clear, in seconds.</summary>
    public int BestSeconds { get; set; }
}

/// <summary>
/// The answer to the reward of a district or a group of mazes, as the client reads it: 0 given, 1 not reached (or no
/// state, or every step taken), 2 cannot be given, 3 no such row.
/// </summary>
public enum SharePointResult : byte
{
    Given = 0,
    NotReached = 1,
    CannotGive = 2,
    Unknown = 3
}

/// <summary>
/// The share points of a person, kept as a document of the person: a state for each district it has been to or cleared a
/// maze of, and one for each group of mazes it has cleared a maze of. A cleared maze adds to its group and to the district of
/// the group, up to what their rows allow; a district has one reward, a group one for each of its steps.
/// </summary>
public sealed class SharePointBook
{
    public List<DistrictProgress> Districts { get; set; } = [];

    public List<MazeGroupProgress> Groups { get; set; } = [];

    /// <summary>
    /// Every maze the person has cleared, with its best rank and time: what entering a later episode asks for.
    /// </summary>
    public List<MazeRecord> Mazes { get; set; } = [];

    /// <summary>
    /// Drops what the tables no longer know: the client looks every district and group up in its table.
    /// </summary>
    public void Normalize(SharePointCatalog catalog)
    {
        var districts = new HashSet<int>();
        var groups = new HashSet<int>();

        Districts.RemoveAll(e => !catalog.TryGet(e.District, out _) || !districts.Add(e.District));
        Groups.RemoveAll(e => !catalog.TryGet(e.Group, out _) || !catalog.TryGet(e.District, out _) || !groups.Add(e.Group));

        foreach (var group in Groups)
        {
            group.Step = Math.Min(group.Step, (byte)SharePointRule.Steps);
        }

        // A group is shown under its district, which the client must know first.
        Groups.RemoveAll(e => !districts.Contains(e.District));

        var mazes = new HashSet<int>();

        Mazes.RemoveAll(e => e.Maze <= 0 || !mazes.Add(e.Maze));
    }

    public MazeRecord? FindMaze(int maze) => Mazes.Find(e => e.Maze == maze);

    /// <summary>
    /// A clear of the maze: one more, and the rank and the time when they are the best yet.
    /// </summary>
    public MazeRecord Record(int maze, byte rank, int seconds)
    {
        var record = FindMaze(maze);

        if (record is null)
        {
            record = new MazeRecord { Maze = maze, BestRank = rank, BestSeconds = seconds };
            Mazes.Add(record);
        }

        record.Clears++;
        record.BestRank = Math.Max(record.BestRank, rank);
        record.BestSeconds = record.Clears == 1 ? seconds : Math.Min(record.BestSeconds, seconds);

        return record;
    }

    public DistrictProgress? FindDistrict(int district) => Districts.Find(e => e.District == district);

    public MazeGroupProgress? FindGroup(int group) => Groups.Find(e => e.Group == group);

    /// <summary>
    /// The person is in a district: it gets a state when it has a row and none yet. Returns the new state, or null.
    /// </summary>
    public DistrictProgress? Visit(SharePointCatalog catalog, int district)
    {
        if (!catalog.TryGet(district, out _) || FindDistrict(district) is not null)
        {
            return null;
        }

        var state = new DistrictProgress { District = district };

        Districts.Add(state);

        return state;
    }

    /// <summary>
    /// A maze of a group is cleared: the group gets the points of the episode of the maze, up to its last step, and the
    /// district of the group what the group got, up to the point of its reward. Returns both states, or null when the group
    /// or its district has no row.
    /// </summary>
    public (DistrictProgress District, MazeGroupProgress Group)? Clear(SharePointCatalog catalog, int maze, int group, int episode)
    {
        if (!catalog.TryGet(group, out var rule) || !rule.IsGroup || !catalog.TryGet(rule.District, out var districtRule))
        {
            return null;
        }

        var add = rule.ClearPoints[Math.Clamp(episode, 0, rule.ClearPoints.Count - 1)];
        var max = rule.MaxPoint;
        var before = 0;
        var gap = add;

        if (FindGroup(group) is { } state)
        {
            before = state.Point;
            state.Point = (int)Math.Min((long)state.Point + add, max);
            gap = Math.Max(0, state.Point - before);
            state.Clears++;
        }

        else
        {
            state = new MazeGroupProgress { Group = group, District = rule.District, Point = add, Clears = 1 };
            Groups.Add(state);
        }

        state.Maze = maze;
        state.District = rule.District;

        var cap = districtRule.Points[0];

        if (FindDistrict(rule.District) is { } district)
        {
            if (before < max)
            {
                district.Point = (int)Math.Min((long)district.Point + gap, cap);
            }
        }

        else
        {
            district = new DistrictProgress { District = rule.District, Point = Math.Min(add, cap) };
            Districts.Add(district);
        }

        return (district, state);
    }

    /// <summary>
    /// What taking the reward of a district would give: its one reward, once its point is at the point of the reward.
    /// </summary>
    public SharePointResult ClaimDistrict(SharePointCatalog catalog, int district, out byte step, out ProgressItem? reward)
    {
        step = 0;
        reward = null;

        if (FindDistrict(district) is not { } state)
        {
            return SharePointResult.NotReached;
        }

        if (state.Rewarded)
        {
            step = 1;
            return SharePointResult.NotReached;
        }

        if (!catalog.TryGet(district, out var rule))
        {
            return SharePointResult.Unknown;
        }

        if (state.Point < rule.Points[0])
        {
            return SharePointResult.NotReached;
        }

        step = 1;
        reward = rule.Rewards[0];

        return SharePointResult.Given;
    }

    /// <summary>
    /// What taking the rewards of a group would give: every step from the one it is at whose points the group has, and
    /// the step it would then be at.
    /// </summary>
    public SharePointResult ClaimGroup(SharePointCatalog catalog, int group, out byte step, out List<ProgressItem> rewards)
    {
        rewards = [];

        if (!catalog.TryGet(group, out var rule))
        {
            step = SharePointRule.Steps;
            return SharePointResult.Unknown;
        }

        if (FindGroup(group) is not { } state)
        {
            step = 0;
            return SharePointResult.NotReached;
        }

        step = state.Step;

        while (step < SharePointRule.Steps && state.Point >= rule.Points[step] && rule.Rewards[step].Item != 0)
        {
            rewards.Add(rule.Rewards[step]);
            step++;
        }

        if (rewards.Count == 0)
        {
            step = state.Step;
            return SharePointResult.NotReached;
        }

        return SharePointResult.Given;
    }
}
