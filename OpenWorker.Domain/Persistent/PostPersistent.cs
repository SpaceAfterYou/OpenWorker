namespace OpenWorker.Domain.Persistent;

/// <summary>
/// Who a letter is for: a person, or an account, which any of its persons can open.
/// </summary>
public enum PostKind : byte
{
    /// <summary>
    /// From one person to another, by name.
    /// </summary>
    Person = 0,

    /// <summary>
    /// From the game or a game master to an account.
    /// </summary>
    Account = 1
}

/// <summary>
/// What has happened to a letter. The low bits are what the client is shown; the two sides of a letter delete it each
/// for themselves, and the row goes when neither keeps it.
/// </summary>
[Flags]
public enum PostState : byte
{
    None = 0,

    Read = 1 << 0,

    /// <summary>
    /// The money and the items have been taken, or there were none. They are kept on the row for the record, never
    /// given again.
    /// </summary>
    Received = 1 << 1,

    /// <summary>
    /// The receiver sent it back, so it lies with its first sender now.
    /// </summary>
    Returned = 1 << 2,

    /// <summary>
    /// The receiver moved it to the box of kept letters.
    /// </summary>
    Saved = 1 << 3,

    /// <summary>
    /// The sender took it off the list of sent letters.
    /// </summary>
    GoneForSender = 1 << 4,

    /// <summary>
    /// The receiver deleted it.
    /// </summary>
    GoneForReceiver = 1 << 5,

    /// <summary>
    /// What the client sees of the state.
    /// </summary>
    Shown = Read | Received | Returned | Saved
}

/// <summary>
/// An item that travels with a letter, as it left the inventory of the sender (or as the game made it). It is not an
/// item of anybody while it travels: the receiver gets it under a serial of its own.
/// </summary>
public sealed class PostItemPersistent
{
    public int Item { get; init; }
    public short Count { get; init; }
    public byte BindType { get; init; }
    public byte Upgrade { get; init; }
    public byte Endurance { get; init; }
    public byte SocketActive { get; init; }
    public DateTime? DueTo { get; init; }
    public byte UpgradeCount { get; init; }
    public byte UpgradeLimit { get; init; }
    public byte Flag { get; init; }
    public int Exp { get; init; }
    public string BroachState { get; init; } = "000000000000000";
    public byte RestoreCount { get; init; }
    public byte SealCount { get; init; }
    public short[] OptionType { get; init; } = new short[ItemPersistent.OptionCount];
    public int[] OptionValue { get; init; } = new int[ItemPersistent.OptionCount];

    /// <summary>
    /// The gems and broaches of the item, as <see cref="ItemPersistent"/> keeps them; missing in the letters written before.
    /// </summary>
    public int[]? Sockets { get; init; }
    public short[]? SocketOptionType { get; init; }
    public int[]? SocketOptionValue { get; init; }
    public int[]? Broaches { get; init; }
}

/// <summary>
/// A letter: text, money and items for one person (or one account), and what happened to it.
/// </summary>
public sealed class PostPersistent : BasicPersistent
{
    public const int MaxTitleLength = 40;
    public const int MaxBodyLength = 400;
    public const int MaxNameLength = 21;
    public const int MaxItems = 5;

    /// <summary>
    /// The serial the client sees.
    /// </summary>
    public long Id { get; init; }

    public PostKind Kind { get; init; }

    /// <summary>
    /// The person who sent it, or nobody when the game did.
    /// </summary>
    public int? SenderId { get; set; }

    public required string SenderName { get; set; }

    public byte SenderHero { get; set; }

    /// <summary>
    /// The person it is for, for a letter of a person.
    /// </summary>
    public int? ReceiverId { get; set; }

    public string ReceiverName { get; set; } = string.Empty;

    public byte ReceiverHero { get; set; }

    /// <summary>
    /// The account it is for, for a letter of an account.
    /// </summary>
    public int? ReceiverAccountId { get; init; }

    public required string Title { get; init; }

    public required string Body { get; init; }

    /// <summary>
    /// Gold for the receiver.
    /// </summary>
    public long Money { get; init; }

    /// <summary>
    /// What the sender paid to send it, kept for the record.
    /// </summary>
    public long Postage { get; init; }

    public List<PostItemPersistent> Items { get; init; } = [];

    /// <summary>
    /// The kind of a letter of the game (the main and sub type of the client table of system letters); 0 for a letter
    /// of a person.
    /// </summary>
    public byte MainType { get; init; }

    public byte SubType { get; init; }

    public PostState State { get; set; }

    /// <summary>
    /// Until when the letter is kept, as the client shows it.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// The money and the items that are still to be taken.
    /// </summary>
    public bool HasAttachments => !State.HasFlag(PostState.Received) && (Money > 0 || Items.Count > 0);
}
