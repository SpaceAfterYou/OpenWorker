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

    /// <summary>
    /// The gems in the sockets of the item, by the place of the socket: the gem item, zero for an empty socket.
    /// </summary>
    public int[] Sockets { get; set; } = [];

    /// <summary>
    /// The options each gem gives, <see cref="OptionCount"/> per socket in the order of <see cref="Sockets"/>.
    /// </summary>
    public short[] SocketOptionType { get; set; } = [];

    public int[] SocketOptionValue { get; set; } = [];

    /// <summary>
    /// The broaches set into the item: three groups of five items, or nothing when none were ever set.
    /// </summary>
    public int[] Broaches { get; set; } = [];

    public PersonPersistent? Person { get; init; }
}
