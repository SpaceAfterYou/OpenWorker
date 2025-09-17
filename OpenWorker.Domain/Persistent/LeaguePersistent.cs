namespace OpenWorker.Domain.Persistent;

public sealed class LeaguePersistent : BasicPersistent
{
    public int Id { get; init; }

    public required short CardEmblem { get; set; }
    public required short CardBorder { get; set; }
    
    public required string Name { get; init; }

    public ICollection<PersonPersistent> MemberList { get; init; } = [];
    
    public required PersonPersistent Master { get; set; }
}