namespace OpenWorker.Domain.Persistent;

public sealed class GatePersistent : BasicPersistent
{
    public short Id { get; init; }

    public string Name { get; set; } = string.Empty;

    public ICollection<PersonPersistent> Persons { get; } = [];

    // internal static GatePersistent Empty => new();
}