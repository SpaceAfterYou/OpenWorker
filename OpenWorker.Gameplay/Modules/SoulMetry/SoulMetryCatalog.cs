using System.Collections.ObjectModel;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.SoulMetry;

/// <summary>
/// One thing to find in a maze: the interaction object, the maze it stands in and how often it shows up.
/// </summary>
public readonly record struct SoulMetryObject(int ObjectId, int Maze, int SpawnRate);

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
        int[] mazes = [row.Maze1, row.Maze2, row.Maze3, row.Maze4, row.Maze5];
        int[] objects = [row.Object1, row.Object2, row.Object3, row.Object4, row.Object5];
        int[] rates = [row.SpawnRate1, row.SpawnRate2, row.SpawnRate3, row.SpawnRate4, row.SpawnRate5];

        var found = new List<SoulMetryObject>();

        for (var i = 0; i < objects.Length; i++)
        {
            if (objects[i] != 0)
            {
                found.Add(new SoulMetryObject(objects[i], mazes[i], rates[i]));
            }
        }

        var rewards = new List<SoulMetryReward>();

        if (row.RewardItem1 > 0)
        {
            rewards.Add(new SoulMetryReward(row.RewardType1, row.RewardItem1, row.RewardCount1));
        }

        if (row.RewardItem2 > 0)
        {
            rewards.Add(new SoulMetryReward(row.RewardType2, row.RewardItem2, row.RewardCount2));
        }

        return new SoulMetryDefinition(row.Id, row.CompleteBit, found, rewards);
    }
}
