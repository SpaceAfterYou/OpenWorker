namespace OpenWorker.Domain.Persistent;

public abstract class BasicPersistent
{
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}