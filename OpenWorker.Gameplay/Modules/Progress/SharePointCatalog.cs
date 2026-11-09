using System.Collections.ObjectModel;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Progress;

/// <summary>
/// A district or a group of mazes of <c>tb_DS_Point</c>: what a clear of one of its mazes adds, by the episode of the maze,
/// the points of its steps (a district has one), and the reward of each step.
/// </summary>
public sealed record SharePointRule(
    int Id,
    bool IsGroup,
    int District,
    IReadOnlyList<int> ClearPoints,
    IReadOnlyList<int> Points,
    IReadOnlyList<ProgressItem> Rewards)
{
    public const int Steps = SharePointRow.Steps;

    /// <summary>
    /// The most points the group can have: those of its last step.
    /// </summary>
    public int MaxPoint => Points[Steps - 1];
}

/// <summary>
/// The districts and the groups of mazes that have share points. The rows are the server's own (<see cref="SharePointRow"/>),
/// which say the district of a group and what a clear adds; only those the client's table has are kept, as the client
/// looks every district and group it is told about up in its own table. Without the server's table the client's rows are
/// used: a district then gets its state and its reward, but no clear adds anything, as nothing says what it adds.
/// </summary>
public sealed class SharePointCatalog
{
    private readonly Dictionary<int, SharePointRule> _rules = [];

    public SharePointCatalog(ReadOnlyCollection<DSPointRow> client, ReadOnlyCollection<SharePointRow> server)
        : this(server.Count > 0 ? FromServer(client, server) : FromClient(client))
    {
    }

    /// <summary>
    /// A catalog of the districts and groups given.
    /// </summary>
    public static SharePointCatalog Of(IEnumerable<SharePointRule> rules)
    {
        return new SharePointCatalog(rules);
    }

    private SharePointCatalog(IEnumerable<SharePointRule> rules)
    {
        foreach (var rule in rules)
        {
            _rules.TryAdd(rule.Id, rule);
        }
    }

    public bool TryGet(int id, out SharePointRule rule)
    {
        return _rules.TryGetValue(id, out rule!);
    }

    private static IEnumerable<SharePointRule> FromServer(ReadOnlyCollection<DSPointRow> client, ReadOnlyCollection<SharePointRow> server)
    {
        var known = client.Select(e => (int)e.Id).ToHashSet();

        foreach (var row in server)
        {
            if (!known.Contains(row.Id))
            {
                continue;
            }

            var rewards = new ProgressItem[SharePointRule.Steps];

            for (var step = 0; step < rewards.Length; step++)
            {
                rewards[step] = new ProgressItem(row.ItemKinds[step], row.Items[step], row.ItemCounts[step]);
            }

            yield return new SharePointRule(
                row.Id,
                row.Kind != 0,
                row.District,
                row.ClearPoints.Select(e => (int)e).ToArray(),
                row.Points.Select(e => (int)e).ToArray(),
                rewards);
        }
    }

    private static IEnumerable<SharePointRule> FromClient(ReadOnlyCollection<DSPointRow> client)
    {
        foreach (var row in client)
        {
            yield return new SharePointRule(
                (ushort)row.Id,
                row.Kind != 0,
                0,
                new int[SharePointRule.Steps],
                new[] { row.Point1, row.Point2, row.Point3, row.Point4, row.Point5 }.Select(e => (int)(ushort)e).ToArray(),
                [
                    new ProgressItem(row.ItemKind1, row.Item1, row.ItemCount1),
                    new ProgressItem(row.ItemKind2, row.Item2, row.ItemCount2),
                    new ProgressItem(row.ItemKind3, row.Item3, row.ItemCount3),
                    new ProgressItem(row.ItemKind4, row.Item4, row.ItemCount4),
                    new ProgressItem(row.ItemKind5, row.Item5, row.ItemCount5)
                ]);
        }
    }
}
