using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Helpers;

/// <summary>
/// The helpers the tables know. A helper is a character of the game that fights beside the person.
/// </summary>
public sealed class HelperCatalog
{
    private readonly Dictionary<int, int> _numbers;

    public HelperCatalog(ReadOnlyCollection<HelperRow> rows) : this(rows.ToDictionary(e => e.Id, e => (int)e.Field1))
    {
    }

    private HelperCatalog(Dictionary<int, int> numbers)
    {
        _numbers = numbers;
    }

    public static HelperCatalog From(IEnumerable<int> helpers) => new(helpers.ToDictionary(e => e, e => e));

    public IEnumerable<int> All => _numbers.Keys;

    public bool Contains(int helper) => _numbers.ContainsKey(helper);
}

/// <summary>
/// A helper the person has. The order is its place in the list of the client.
/// </summary>
public sealed class HelperEntry
{
    public int Helper { get; set; }

    public byte Order { get; set; }
}

/// <summary>
/// The helpers of a person. This is what is stored.
/// </summary>
public sealed class HelperBook
{
    public List<HelperEntry> Helpers { get; set; } = [];

    /// <summary>
    /// Whether helpers are called by themselves when the person enters a maze.
    /// </summary>
    public bool AutoSummon { get; set; }

    /// <summary>
    /// What the person offers its friends as support, or 0 if it does not.
    /// </summary>
    public byte SupportType { get; set; }

    /// <summary>
    /// Writes of the book to the database go one after another, so the last one has the latest state.
    /// </summary>
    [JsonIgnore]
    public SemaphoreSlim Gate { get; } = new(1, 1);

    public HelperEntry? Find(int helper) => Helpers.Find(e => e.Helper == helper);

    /// <summary>
    /// Gives the person a helper at the end of the list. False if it has it already.
    /// </summary>
    public bool TryAdd(int helper, out HelperEntry entry)
    {
        entry = null!;

        if (Find(helper) is not null)
        {
            return false;
        }

        entry = new HelperEntry { Helper = helper, Order = (byte)(Helpers.Count == 0 ? 0 : Helpers.Max(e => e.Order) + 1) };
        Helpers.Add(entry);

        return true;
    }

    /// <summary>
    /// Two helpers swap their places. False if the person does not have both.
    /// </summary>
    public bool TrySwap(int first, int second, out HelperEntry one, out HelperEntry two)
    {
        one = Find(first)!;
        two = Find(second)!;

        if (one is null || two is null || first == second)
        {
            return false;
        }

        (one.Order, two.Order) = (two.Order, one.Order);

        return true;
    }

    /// <summary>
    /// Drops the helpers the tables do not know and gives every helper its own place.
    /// </summary>
    public void Normalize(HelperCatalog catalog)
    {
        Helpers = Helpers.Where(e => catalog.Contains(e.Helper)).DistinctBy(e => e.Helper).OrderBy(e => e.Order).ToList();

        for (var index = 0; index < Helpers.Count; index++)
        {
            Helpers[index].Order = (byte)index;
        }
    }
}
