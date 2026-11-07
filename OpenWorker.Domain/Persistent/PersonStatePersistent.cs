using OpenWorker.Domain.Enums;

namespace OpenWorker.Domain.Persistent;

/// <summary>
/// A small document with the progress of one feature of a person. The feature that owns the kind
/// decides what the payload holds, so a change of the format never needs a new table.
/// </summary>
public sealed class PersonStatePersistent : BasicPersistent
{
    public int PersonId { get; init; }

    public PersonStateKind Kind { get; init; }

    public required string Payload { get; set; }
}
