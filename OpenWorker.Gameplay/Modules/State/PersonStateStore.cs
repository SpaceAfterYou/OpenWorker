using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Persistence;

namespace OpenWorker.Gameplay.Modules.State;

/// <summary>
/// Reads and writes the progress documents of a person (<see cref="PersonStatePersistent"/>). The features read theirs
/// when the person enters; they are written by the flush of the person (<c>PersistenceFlushService</c>).
/// </summary>
public sealed class PersonStateStore(IDbContextFactory<PersistenceContext> factory)
{
    internal static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.General)
    {
        IncludeFields = false,
        WriteIndented = false
    };

    public async ValueTask<T?> LoadAsync<T>(int person, PersonStateKind kind, CancellationToken cancellationToken)
        where T : class
    {
        await using var database = await factory
            .CreateDbContextAsync(cancellationToken);

        var payload = await database.PersonStates
            .Where(e => e.PersonId == person && e.Kind == kind)
            .Select(e => e.Payload)
            .FirstOrDefaultAsync(cancellationToken);

        return payload is null ? null : JsonSerializer.Deserialize<T>(payload, Options);
    }

    /// <summary>
    /// Writes one document now, in one statement whether the row is there or not.
    /// </summary>
    public async ValueTask SaveAsync<T>(int person, PersonStateKind kind, T value, CancellationToken cancellationToken)
        where T : class
    {
        var payload = JsonSerializer.Serialize(value, Options);

        await factory.SaveChangesAsync(new PersonChanges { Person = person, Documents = [new PersonDocument(kind, payload)] }, cancellationToken);
    }
}
