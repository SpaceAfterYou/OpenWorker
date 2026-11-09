
namespace OpenWorker.Gameplay.Modules.Boosters;

/// <summary>
/// A booster the person has. For a booster that ends at a moment, <see cref="Remain"/> is that moment in Unix seconds;
/// for the others it is the milliseconds left. This is what is stored.
/// </summary>
public sealed class BoosterEntry
{
    public short Booster { get; set; }

    public long Remain { get; set; }

    /// <summary>
    /// Whether the booster belongs to the account, so every person of it has it.
    /// </summary>
    public bool Account { get; set; }
}

public sealed class BoosterBook
{
    public List<BoosterEntry> Boosters { get; set; } = [];

    public BoosterEntry? Find(short booster) => Boosters.Find(e => e.Booster == booster);
}
