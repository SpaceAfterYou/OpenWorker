namespace OpenWorker.Domain.Persistent;

public sealed class ItemPersistent : BasicPersistent
{
    public long Serial { get; init; }
    // public StorageGroup Storage { get; set; }
    public int Count { get; init; }
    public short Slot { get; init; }
}