namespace OpenWorker.Domain.Persistent;

/// <summary>
/// An item of a person and the place it lies in. The id is the serial the client sees.
/// </summary>
public sealed class ItemPersistent : BasicPersistent
{
    public const int OptionCount = 5;

    public long Id { get; init; }

    public int PersonId { get; init; }

    /// <summary>
    /// The storage group of the item, the same numbers the client uses.
    /// </summary>
    public byte Storage { get; set; }

    public short Slot { get; set; }

    public int Item { get; init; }

    public short Count { get; set; }

    public byte BindType { get; set; }
    public byte Upgrade { get; set; }
    public byte Endurance { get; set; }
    public byte SocketActive { get; set; }
    public DateTime? DueTo { get; set; }
    public byte UpgradeCount { get; set; }
    public byte UpgradeLimit { get; set; }
    public byte Flag { get; set; }
    public int Exp { get; set; }
    public string BroachState { get; set; } = "000000000000000";
    public byte RestoreCount { get; set; }
    public byte SealCount { get; set; }

    public short[] OptionType { get; set; } = new short[OptionCount];
    public int[] OptionValue { get; set; } = new int[OptionCount];

    public PersonPersistent? Person { get; init; }
}
