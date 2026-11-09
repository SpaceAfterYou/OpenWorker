using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;

namespace OpenWorker.Domain.Stores;

/// <summary>
/// What changed for one person since the last save: items to write and remove, the numbers kept on the person,
/// where it stands and the progress documents of its features. Each part is the state at the moment it was taken,
/// so writing the same part twice, or an older part after a newer one of another person, is harmless.
/// </summary>
public sealed class PersonChanges
{
    public required int Person { get; init; }

    /// <summary>
    /// Items the database has not seen yet. They are written the same way as <see cref="Changed"/>: a retry of a save
    /// that did reach the database finds them there and rewrites them.
    /// </summary>
    public IReadOnlyList<ItemPersistent> Added { get; init; } = [];

    public IReadOnlyList<ItemPersistent> Changed { get; init; } = [];
    public IReadOnlyList<long> Removed { get; init; } = [];
    public PersonStateChanges? State { get; init; }
    public PersonPlace? Place { get; init; }
    public IReadOnlyList<PersonDocument> Documents { get; init; } = [];

    public bool IsEmpty => Added.Count == 0 && Changed.Count == 0 && Removed.Count == 0 && State is null && Place is null &&
                           Documents.Count == 0;
}

/// <summary>
/// The numbers of a person kept in its row: level, experience, the wallet, how far its storages are extended
/// and its quick slots.
/// </summary>
public sealed record PersonStateChanges(
    byte Level,
    int Experience,
    long Gold,
    long BattlePoint,
    long Ether,
    long[] Currencies,
    byte[] StorageGrades,
    int[] QuickSlotCards,
    int[] QuickSlotItems);

/// <summary>
/// Where a person stands in a district, kept so that the next entry from the gate starts there.
/// </summary>
public readonly record struct PersonPlace(short Location, float X, float Y, float Z, float Rotation);

/// <summary>
/// The progress document of one feature of a person (<see cref="PersonStatePersistent"/>), already serialized.
/// </summary>
public readonly record struct PersonDocument(PersonStateKind Kind, string Payload);
