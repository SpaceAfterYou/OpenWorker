namespace OpenWorker.Domain.Persistent;

public sealed class AccountPersistent : BasicPersistent
{
    public int Id { get; init; }

    public required string Username { get; init; } = string.Empty;

    public ulong Discord { get; init; }

    public required byte[] PasswordHash { get; set; }
    public required byte[] SaltHash { get; set; }

    public DateTime LastLogin { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// What the account may do beyond playing: 0 a player, 1 a game master, 2 who manages the game. The game shows it on
    /// the person and the test commands of the chat ask for it.
    /// </summary>
    public byte AccessLevel { get; set; }

    /// <summary>
    /// The password asked before a trade, hashed as the password of the account; empty while the account has none.
    /// </summary>
    public string TradePassword { get; set; } = string.Empty;

    /// <summary>
    /// The wrong trade passwords given on the account since the last right one (or since the password was made). The account
    /// keeps the count, not the login: a lock outlasts the session that earned it.
    /// </summary>
    public short TradePasswordFailed { get; set; }

    /// <summary>
    /// When the last wrong trade password was given; empty while the count is zero.
    /// </summary>
    public DateTime? TradePasswordFailedAt { get; set; }

    public string SecondPassword { get; set; } = string.Empty;

    /// <summary>
    /// The currency of the cash shop, shared by the characters of the account. Only a game master gives it.
    /// </summary>
    public long Cash { get; set; }

    /// <summary>
    /// The options of the client (the system menu). The client treats them as one set for the account.
    /// </summary>
    public byte[] OptionList { get; set; } = [];

    public List<PersonPersistent> Persons { get; init; } = [];
}