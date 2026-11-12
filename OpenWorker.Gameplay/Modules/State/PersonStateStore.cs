using System.Text.Json;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;

namespace OpenWorker.Gameplay.Modules.State;

/// <summary>
/// Reads and writes the progress documents of a person (<see cref="PersonStatePersistent"/>). The features read theirs
/// when the person enters; they are written by the flush of the person (<c>PersistenceFlushService</c>). The rows are
/// read and written through <see cref="IPersonStore"/>.
/// <para>
/// An entry reads every document of the person in one query (<see cref="ReadAtEntryAsync"/>), and each feature that loads
/// its document while the entry lasts takes it from what was read, whatever its kind. Used on the world loop only.
/// </para>
/// </summary>
public sealed class PersonStateStore(IPersonStore store)
{
    internal static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.General)
    {
        IncludeFields = false,
        WriteIndented = false
    };

    /// <summary>
    /// The documents read for the persons who are entering now.
    /// </summary>
    private readonly Dictionary<int, EntryDocuments> _entering = [];

    /// <summary>
    /// Reads every document of the person in one query and keeps them until the returned scope is disposed, at the end
    /// of the entry: a load of a kind in that time takes the document from what was read, or nothing when the person has
    /// none of that kind, without a query of its own.
    /// </summary>
    public async ValueTask<EntryDocuments> ReadAtEntryAsync(int person, CancellationToken cancellationToken)
    {
        var documents = new EntryDocuments(this, person, await store.LoadDocumentsAsync(person, cancellationToken));

        _entering[person] = documents;

        return documents;
    }

    public async ValueTask<T?> LoadAsync<T>(int person, PersonStateKind kind, CancellationToken cancellationToken)
        where T : class
    {
        var payload = _entering.TryGetValue(person, out var documents) && documents.TryTake(kind, out var read)
            ? read
            : await store.LoadDocumentAsync(person, kind, cancellationToken);

        return payload is null ? null : JsonSerializer.Deserialize<T>(payload, Options);
    }

    /// <summary>
    /// Writes one document now, in one statement whether the row is there or not.
    /// </summary>
    public async ValueTask SaveAsync<T>(int person, PersonStateKind kind, T value, CancellationToken cancellationToken)
        where T : class
    {
        // What the entry read of this kind is older than this: a later load reads the row again.
        if (_entering.TryGetValue(person, out var documents))
        {
            documents.TryTake(kind, out _);
        }

        var payload = JsonSerializer.Serialize(value, Options);

        await store.SaveChangesAsync(new PersonChanges { Person = person, Documents = [new PersonDocument(kind, payload)] }, cancellationToken);
    }

    /// <summary>
    /// The documents of one person read at its entry. Each kind is given out once: the feature that took it keeps it in
    /// its component from then on, and the flush writes it from there, so a second load of the same kind reads the row
    /// again instead of what may be older by then.
    /// </summary>
    public sealed class EntryDocuments : IDisposable
    {
        private readonly PersonStateStore _owner;
        private readonly int _person;
        private readonly Dictionary<PersonStateKind, string> _documents;
        private readonly HashSet<PersonStateKind> _taken = [];

        internal EntryDocuments(PersonStateStore owner, int person, Dictionary<PersonStateKind, string> documents)
        {
            _owner = owner;
            _person = person;
            _documents = documents;
        }

        /// <summary>
        /// The document of the kind as read at the entry, or null when the person had none; false when it was given out
        /// already.
        /// </summary>
        internal bool TryTake(PersonStateKind kind, out string? payload)
        {
            payload = null;

            if (!_taken.Add(kind))
            {
                return false;
            }

            payload = _documents.GetValueOrDefault(kind);

            return true;
        }

        /// <summary>
        /// The entry is over: the loads that come later read the rows.
        /// </summary>
        public void Dispose()
        {
            if (_owner._entering.TryGetValue(_person, out var current) && ReferenceEquals(current, this))
            {
                _owner._entering.Remove(_person);
            }
        }
    }
}
