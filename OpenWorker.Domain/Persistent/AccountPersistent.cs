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
    public string SecondPassword { get; set; } = string.Empty;

    /// <summary>
    /// The options of the client (the system menu). The client treats them as one set for the account.
    /// </summary>
    public byte[] OptionList { get; set; } = [];

    public List<PersonPersistent> Persons { get; init; } = [];
}