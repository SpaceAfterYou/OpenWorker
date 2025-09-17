namespace OpenWorker.Domain.Persistent;

public sealed class AccountPersistent : BasicPersistent
{
    public int Id { get; init; }

    public required string Username { get; init; } = string.Empty;

    public ulong Discord { get; init; }

    public required byte[] PasswordHash { get; set; }
    public required byte[] SaltHash { get; set; }

    public DateTime LastLogin { get; set; } = DateTime.UtcNow;
    
    public string TradePassword { get; set; } = string.Empty;
    public string SecondPassword { get; set; } = string.Empty;

    public List<PersonPersistent> Persons { get; init; } = [];
}