using System.Collections.ObjectModel;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.SoulMetry;

/// <summary>
/// One thing to find in a maze: the interaction object, the maze it stands in and how often it shows up.
/// </summary>
public readonly record struct SoulMetryObject(int Object, int Maze, int SpawnRate);

/// <summary>
/// A reward of a soul metry. The type tells what the item column holds: 1 is an item, 2 is a row of the
/// quest reward table with an item for every class.
/// </summary>
public readonly record struct SoulMetryReward(byte Type, int Item, byte Count);

public sealed record SoulMetryDefinition(
    short Id,
    int CompleteBit,
    IReadOnlyList<SoulMetryObject> Objects,
    IReadOnlyList<SoulMetryReward> Rewards)
{
    /// <summary>
    /// The bits of all objects of the soul metry, set when every one is found.
    /// </summary>
    public const short AllFound = 1023;

    /// <summary>
    /// The bits the person has when the soul metry is added: the ones of the missing objects are set.
    /// </summary>
    public short InitialBits
    {
        get
        {
            var bits = AllFound;

            for (var i = 0; i < Objects.Count; i++)
            {
                bits &= (short)~(1 << i);
            }

            return bits;
        }
    }
}

public sealed class SoulMetryCatalog
{
    private readonly Dictionary<short, SoulMetryDefinition> _byId;
    private readonly Dictionary<int, SoulMetryDefinition> _byBit;

    public SoulMetryCatalog(ReadOnlyCollection<SoulMetryRow> rows) : this(rows.Select(Create).ToArray())
    {
    }

    private SoulMetryCatalog(SoulMetryDefinition[] definitions)
    {
        _byId = definitions.ToDictionary(e => e.Id);
        _byBit = _byId.Values.GroupBy(e => e.CompleteBit).ToDictionary(e => e.Key, e => e.First());
    }

    public static SoulMetryCatalog From(IEnumerable<SoulMetryDefinition> definitions) => new(definitions.ToArray());

    public IEnumerable<SoulMetryDefinition> All => _byId.Values;

    public bool TryGet(short id, out SoulMetryDefinition definition) => _byId.TryGetValue(id, out definition!);

    public bool TryGetByBit(int bit, out SoulMetryDefinition definition) => _byBit.TryGetValue(bit, out definition!);

    // The table has five slots of every column, an empty slot has the object 0.
    internal static SoulMetryDefinition Create(SoulMetryRow row)
    {
        int[] mazes = [row.Field4, row.Field5, row.Field6, row.Field7, row.Field8];
        int[] objects = [row.Field9, row.Field10, row.Field11, row.Field12, row.Field13];
        int[] rates = [row.Field14, row.Field15, row.Field16, row.Field17, row.Field18];

        var found = new List<SoulMetryObject>();

        for (var i = 0; i < objects.Length; i++)
        {
            if (objects[i] != 0)
            {
                found.Add(new SoulMetryObject(objects[i], mazes[i], rates[i]));
            }
        }

        var rewards = new List<SoulMetryReward>();

        if (row.Field31 > 0)
        {
            rewards.Add(new SoulMetryReward(row.Field29, row.Field31, row.Field33));
        }

        if (row.Field32 > 0)
        {
            rewards.Add(new SoulMetryReward(row.Field30, row.Field32, row.Field34));
        }

        return new SoulMetryDefinition(row.Id, row.Field3, found, rewards);
    }
}
