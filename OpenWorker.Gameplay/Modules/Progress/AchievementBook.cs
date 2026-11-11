using OpenWorker.Hotspot.Modules.Persons.Types;

namespace OpenWorker.Gameplay.Modules.Progress;

/// <summary>
/// Where the counter of one chain of achievements stands: the achievement of the chain it counts toward and how much it has
/// counted since the chain began.
/// </summary>
public sealed class AchievementCounter
{
    public ushort Type { get; set; }

    public int Target { get; set; }

    public int Achievement { get; set; }

    public long Count { get; set; }
}

/// <summary>
/// What may come of the reward of an achievement.
/// </summary>
public enum AchievementClaim
{
    Ready,

    /// <summary>
    /// The tables have no such achievement.
    /// </summary>
    Unknown,

    /// <summary>
    /// The counter has not reached the achievement.
    /// </summary>
    NotReached,

    /// <summary>
    /// The reward was taken before.
    /// </summary>
    Taken
}

/// <summary>
/// The achievements of a person, kept as a document of the person: a counter for each chain that counts, and one bit for
/// each achievement whose reward was taken (by its <see cref="AchievementRule.CompleteBit"/>, 128 bytes as the client takes
/// them). How many achievements of each category are reached is not kept, it follows from the counters.
/// </summary>
public sealed class AchievementBook
{
    public const int RewardBytes = 128;

    /// <summary>
    /// The kinds of event whose counter holds the highest value seen (a level, a combo) rather than a sum.
    /// </summary>
    private static readonly HashSet<ushort> Highest = [(ushort)AchievementType.LevelReached, (ushort)AchievementType.Combo];

    public List<AchievementCounter> Counters { get; set; } = [];

    public byte[] Rewarded { get; set; } = new byte[RewardBytes];

    /// <summary>
    /// Drops what the tables no longer know, so that a changed table leaves no counter the client cannot show.
    /// </summary>
    public void Normalize(AchievementCatalog catalog)
    {
        if (Rewarded is not { Length: RewardBytes })
        {
            var bits = new byte[RewardBytes];

            Rewarded?.AsSpan(0, Math.Min(Rewarded.Length, RewardBytes)).CopyTo(bits);
            Rewarded = bits;
        }

        var keys = new HashSet<(ushort, int)>();

        Counters.RemoveAll(e => !catalog.TryGet(e.Achievement, out var rule) || rule.Type != e.Type || rule.Target != e.Target ||
                                e.Count < 0 || !keys.Add((e.Type, e.Target)));
    }

    /// <summary>
    /// Starts the chains the person has the level for. Returns whether any started.
    /// </summary>
    public bool Open(AchievementCatalog catalog, int level)
    {
        var opened = false;

        foreach (var begin in catalog.Begins)
        {
            if (begin.OpenLevel > level || !catalog.TryGet(begin.Achievement, out var first) || Find(first.Type, first.Target) is not null)
            {
                continue;
            }

            Counters.Add(new AchievementCounter { Type = first.Type, Target = first.Target, Achievement = first.Id });
            opened = true;
        }

        return opened;
    }

    /// <summary>
    /// Brings the counter of the level to the level and moves every counter past what it has already reached, as the person
    /// enters. A reward bit of an achievement that is not reached is cleared.
    /// </summary>
    public void CatchUp(AchievementCatalog catalog, int level)
    {
        foreach (var counter in Counters)
        {
            if (counter.Type == (ushort)AchievementType.LevelReached)
            {
                counter.Count = level;
            }

            if (!catalog.TryGet(counter.Achievement, out var rule))
            {
                continue;
            }

            while (counter.Count >= rule.Count && catalog.Next(rule) is { } next)
            {
                rule = next;
                counter.Achievement = next.Id;
            }

            for (var ahead = rule; ahead is not null; ahead = catalog.Next(ahead))
            {
                if (ahead.Count > counter.Count)
                {
                    SetRewarded(ahead.CompleteBit, false);
                }
            }
        }
    }

    public AchievementCounter? Find(ushort type, int target)
    {
        return Counters.Find(e => e.Type == type && e.Target == target);
    }

    /// <summary>
    /// Counts an event for the chain of its type and target. Returns what the client is told, or null when the person has
    /// no such chain or the chain is at its end. The achievements the count reached are added to <paramref name="reached"/>.
    /// </summary>
    public AchieveUpdateValue? Raise(AchievementCatalog catalog, ushort type, int target, long amount, List<int>? reached = null)
    {
        if (amount <= 0 || Find(type, target) is not { } counter || !catalog.TryGet(counter.Achievement, out var rule) ||
            counter.Count >= rule.Count)
        {
            return null;
        }

        if (Highest.Contains(type))
        {
            if (counter.Count >= amount)
            {
                return null;
            }

            counter.Count = amount;
        }

        else
        {
            counter.Count = counter.Count > long.MaxValue - amount ? long.MaxValue : counter.Count + amount;
        }

        var update = new AchieveUpdateValue { Index = rule.Id, Count = counter.Count };

        if (counter.Count < rule.Count)
        {
            return update;
        }

        // Reached: the counter moves on to the next achievement of the chain, past every one it has reached as well.
        reached?.Add(rule.Id);

        var current = rule;

        while (catalog.Next(current) is { } next)
        {
            current = next;
            counter.Achievement = next.Id;
            update = update with { NextIndex = next.Id, Category = next.Category };

            if (next.Count > counter.Count)
            {
                return update with { CategoryCount = CategoryCount(catalog, next.Category) };
            }

            reached?.Add(next.Id);
        }

        // The chain is done: the counter stays on its last achievement, at its count.
        counter.Count = current.Count;

        return update with
        {
            Count = current.Count,
            NextIndex = current.Id,
            Category = current.Category,
            CategoryCount = CategoryCount(catalog, current.Category)
        };
    }

    /// <summary>
    /// Whether the reward of an achievement can be taken now.
    /// </summary>
    public AchievementClaim Check(AchievementCatalog catalog, int achievement, out AchievementRule rule)
    {
        if (!catalog.TryGet(achievement, out rule) || !IsBit(rule.CompleteBit))
        {
            return AchievementClaim.Unknown;
        }

        // The counter of the key must be the one of this chain: another chain of the same key counts other things.
        if (Find(rule.Type, rule.Target) is not { } counter || catalog.ChainOf(counter.Achievement) != catalog.ChainOf(rule.Id) ||
            counter.Count < rule.Count)
        {
            return AchievementClaim.NotReached;
        }

        return IsRewarded(rule.CompleteBit) ? AchievementClaim.Taken : AchievementClaim.Ready;
    }

    public bool IsRewarded(int bit)
    {
        return IsBit(bit) && (Rewarded[bit / 8] & (1 << (bit % 8))) != 0;
    }

    public void SetRewarded(int bit, bool rewarded = true)
    {
        if (!IsBit(bit))
        {
            return;
        }

        if (rewarded)
        {
            Rewarded[bit / 8] |= (byte)(1 << (bit % 8));
        }

        else
        {
            Rewarded[bit / 8] &= (byte)~(1 << (bit % 8));
        }
    }

    /// <summary>
    /// How many achievements of each category are reached.
    /// </summary>
    public ushort[] Categories(AchievementCatalog catalog)
    {
        var counts = new ushort[AchievementCatalog.Categories];

        foreach (var counter in Counters)
        {
            for (var id = catalog.ChainOf(counter.Achievement); catalog.TryGet(id, out var rule) && id <= counter.Achievement; id++)
            {
                if ((id < counter.Achievement || counter.Count >= rule.Count) && rule.Category < counts.Length)
                {
                    counts[rule.Category]++;
                }
            }
        }

        return counts;
    }

    /// <summary>
    /// Where every counter stands, as the client takes it.
    /// </summary>
    public AchieveValue[] Values()
    {
        return Counters
            .OrderBy(e => e.Achievement)
            .Select(e => new AchieveValue { Index = e.Achievement, Count = e.Count })
            .ToArray();
    }

    private ushort CategoryCount(AchievementCatalog catalog, byte category)
    {
        var counts = Categories(catalog);

        return category < counts.Length ? counts[category] : (ushort)0;
    }

    private static bool IsBit(int bit) => bit > 0 && bit / 8 < RewardBytes;
}
