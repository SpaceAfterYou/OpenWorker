using System.Collections.ObjectModel;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Progress;

/// <summary>
/// An item a reward gives: <see cref="Kind"/> 2 names a row of <c>tb_Quest_Reward</c>, which has an item for each class;
/// anything else is the item itself.
/// </summary>
public readonly record struct ProgressItem(byte Kind, int Item, short Count)
{
    public const byte ByClass = 2;
}

/// <summary>
/// One achievement of <c>tb_Achievement</c>. Achievements come in chains of rows that follow each other by their number
/// (a step of the chain is the next number), all counting the same <see cref="Type"/> of event about the same
/// <see cref="Target"/>: one counter of the person goes through the whole chain, and a row is reached when the counter is at
/// its <see cref="Count"/>.
/// </summary>
public sealed record AchievementRule(
    int Id,
    byte Category,
    int CompleteBit,
    ushort Type,
    int Target,
    long Count,
    long Gold,
    long Ether,
    long BattlePoints,
    IReadOnlyList<int> Titles,
    IReadOnlyList<ProgressItem> Items);

/// <summary>
/// The first achievement of a chain, and the level a person has to be at for the chain to start counting
/// (<c>tb_Achievement_begin</c>).
/// </summary>
public readonly record struct AchievementBegin(int Achievement, int OpenLevel);

/// <summary>
/// The achievements the server counts. The rows come from the client's table, which is what the window of the client shows;
/// the target of a row comes from the server's own table (<see cref="AchievementTargetRow"/>). Without that table every row
/// counts its event about anything, and the chains that would then count the same thing (the kills of one faction or of
/// another) are left out instead of counting each other's events.
/// </summary>
public sealed class AchievementCatalog
{
    /// <summary>
    /// The categories of the window; the client keeps a count of the achievements reached for each.
    /// </summary>
    public const int Categories = 7;

    private readonly Dictionary<int, AchievementRule> _rules = [];
    private readonly Dictionary<int, int> _chains = [];
    private readonly List<AchievementBegin> _begins = [];

    public AchievementCatalog(
        ReadOnlyCollection<AchievementRow> rows,
        ReadOnlyCollection<AchievementTargetRow> targets,
        ReadOnlyCollection<AchievementBeginRow> begins)
        : this(ToRules(rows, targets), begins.Select(e => new AchievementBegin(e.Achievement, e.OpenLevel)), targets.Count > 0)
    {
    }

    /// <summary>
    /// A catalog of the achievements given.
    /// </summary>
    /// <param name="rules">The achievements.</param>
    /// <param name="begins">The first achievement of each chain.</param>
    /// <param name="targeted">Whether the targets of the rows are known; when not, chains that count the same type of
    /// event are left out.</param>
    public static AchievementCatalog Of(IEnumerable<AchievementRule> rules, IEnumerable<AchievementBegin> begins, bool targeted = true)
    {
        return new AchievementCatalog(rules, begins, targeted);
    }

    private AchievementCatalog(IEnumerable<AchievementRule> rules, IEnumerable<AchievementBegin> begins, bool targeted)
    {
        foreach (var rule in rules)
        {
            _rules.TryAdd(rule.Id, rule);
        }

        foreach (var rule in _rules.Values)
        {
            var first = rule.Id;

            while (_rules.ContainsKey(first - 1))
            {
                first--;
            }

            _chains[rule.Id] = first;
        }

        var opened = begins
            .Where(e => _rules.ContainsKey(e.Achievement))
            .ToList();

        var shared = opened
            .GroupBy(e => KeyOf(_rules[e.Achievement]))
            .Where(e => e.Count() > 1)
            .Select(e => e.Key)
            .ToHashSet();

        var keys = new HashSet<(ushort, int)>();

        foreach (var begin in opened)
        {
            var key = KeyOf(_rules[begin.Achievement]);

            // One counter goes through one chain: a second chain of the same key would move on with the first.
            if (!targeted && shared.Contains(key) || !keys.Add(key))
            {
                continue;
            }

            _begins.Add(begin);
        }
    }

    public IReadOnlyList<AchievementBegin> Begins => _begins;

    public bool TryGet(int id, out AchievementRule rule)
    {
        return _rules.TryGetValue(id, out rule!);
    }

    /// <summary>
    /// The achievement that follows in the chain, or null at its end.
    /// </summary>
    public AchievementRule? Next(AchievementRule rule)
    {
        return _rules.GetValueOrDefault(rule.Id + 1);
    }

    /// <summary>
    /// The first achievement of the chain of an achievement.
    /// </summary>
    public int ChainOf(int id)
    {
        return _chains.GetValueOrDefault(id);
    }

    public static (ushort Type, int Target) KeyOf(AchievementRule rule) => (rule.Type, rule.Target);

    private static IEnumerable<AchievementRule> ToRules(ReadOnlyCollection<AchievementRow> rows, ReadOnlyCollection<AchievementTargetRow> targets)
    {
        var server = targets.IndexBy(e => e.Id);

        foreach (var row in rows)
        {
            var hasTarget = server.TryGet(row.Id, out var target);

            yield return new AchievementRule(
                row.Id,
                row.Category,
                row.CompleteBit,
                hasTarget ? target.Type : (ushort)row.Type,
                hasTarget ? target.Target : 0,
                (uint)row.Count,
                (uint)row.Gold,
                (uint)row.Ether,
                (uint)row.BattlePoint,
                new[] { row.Title1, row.Title2 }.Where(e => e > 0).ToArray(),
                new[]
                    {
                        new ProgressItem(row.ItemKind1, row.Item1, row.ItemCount1),
                        new ProgressItem(row.ItemKind2, row.Item2, row.ItemCount2)
                    }
                    .Where(e => e.Item > 0 && e.Count > 0)
                    .ToArray());
        }
    }
}
