using System.Collections.ObjectModel;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Boosters;

/// <summary>
/// What a booster changes.
/// </summary>
public enum BoosterEffect : byte
{
    None = 0,
    IncExp = 1,
    IncItemDrop = 2,
    ReduceDec = 3,
    AddFP = 4,
    IncUpgradeItem = 5,
    IncGoldDrop = 6,
    IncEtherDrop = 7,
    IncBattlePoint = 8,
    Status = 9,
    MazeRewardIncExp = 10,
    MazeRewardIncMoney = 11,
    HiddenEvent = 12,
    FatigueDecrease = 13,
    IncExchangeCount = 14,
    FreeExchangeCommission = 15,
    IncExchangeExpire = 16,
    IncGroutonRate = 17,
    ReduceExchangeCommission = 18,
    CompleteQuestIncExp = 19
}

/// <summary>
/// How a booster uses up its time.
/// </summary>
public enum BoosterTime : byte
{
    /// <summary>
    /// It never runs out.
    /// </summary>
    Endless = 0,

    /// <summary>
    /// The time goes only in districts.
    /// </summary>
    District = 1,

    /// <summary>
    /// The time goes only in mazes.
    /// </summary>
    Maze = 2,

    /// <summary>
    /// The time goes as long as the person is in the game.
    /// </summary>
    Online = 3,

    /// <summary>
    /// It ends at a moment of the calendar, even when the person is away.
    /// </summary>
    Until = 4
}

/// <summary>
/// How an effect is added: as a part of the amount or as an amount.
/// </summary>
public enum BoosterApply : byte
{
    Rate = 1,
    Value = 2
}

public readonly record struct BoosterEffectValue(BoosterEffect Effect, BoosterApply Apply, float Value);

public sealed record BoosterDefinition(
    short Id,
    short Group,
    BoosterTime Time,
    int DurationMilliseconds,
    IReadOnlyList<BoosterEffectValue> Effects);

public sealed class BoosterCatalog
{
    private readonly Dictionary<short, BoosterDefinition> _byId;

    public BoosterCatalog(ReadOnlyCollection<BoosterRow> rows) : this(rows.Select(Create).ToArray())
    {
    }

    private BoosterCatalog(BoosterDefinition[] definitions)
    {
        _byId = definitions.ToDictionary(e => e.Id);
    }

    public static BoosterCatalog From(IEnumerable<BoosterDefinition> definitions) => new(definitions.ToArray());

    public IEnumerable<BoosterDefinition> All => _byId.Values;

    public bool TryGet(short id, out BoosterDefinition definition) => _byId.TryGetValue(id, out definition!);

    // The table has eight slots of every column, an unused slot has no effect.
    internal static BoosterDefinition Create(BoosterRow row)
    {
        byte[] effects = [row.Field2, row.Field3, row.Field4, row.Field5, row.Field6, row.Field7, row.Field8, row.Field9];
        byte[] applies = [row.Field10, row.Field11, row.Field12, row.Field13, row.Field14, row.Field15, row.Field16, row.Field17];
        float[] values = [row.Field18, row.Field19, row.Field20, row.Field21, row.Field22, row.Field23, row.Field24, row.Field25];

        var found = new List<BoosterEffectValue>();

        for (var slot = 0; slot < effects.Length; slot++)
        {
            if (effects[slot] != 0)
            {
                found.Add(new BoosterEffectValue((BoosterEffect)effects[slot], (BoosterApply)applies[slot], values[slot]));
            }
        }

        return new BoosterDefinition(row.Id, row.Field1, (BoosterTime)row.Field35, row.Field36, found);
    }
}
