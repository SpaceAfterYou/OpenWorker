using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Persistence;

namespace OpenWorker.Gameplay.Modules.State;

/// <summary>
/// Reads and writes the progress documents of a person (<see cref="PersonStatePersistent"/>).
/// </summary>
public sealed class PersonStateStore(IDbContextFactory<PersistenceContext> factory)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General)
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
            .AsNoTracking()
            .Where(e => e.PersonId == person && e.Kind == kind)
            .Select(e => e.Payload)
            .FirstOrDefaultAsync(cancellationToken);

        return payload is null ? null : JsonSerializer.Deserialize<T>(payload, Options);
    }

    public async ValueTask SaveAsync<T>(int person, PersonStateKind kind, T value, CancellationToken cancellationToken)
        where T : class
    {
        var payload = JsonSerializer.Serialize(value, Options);

        for (var attempt = 0; ; attempt++)
        {
            await using var database = await factory
                .CreateDbContextAsync(cancellationToken);

            var row = await database.PersonStates
                .FirstOrDefaultAsync(e => e.PersonId == person && e.Kind == kind, cancellationToken);

            if (row is null)
            {
                database.PersonStates.Add(new PersonStatePersistent { PersonId = person, Kind = kind, Payload = payload });
            }

            else
            {
                row.Payload = payload;
                row.UpdatedAt = DateTime.UtcNow;
            }

            try
            {
                await database.SaveChangesAsync(cancellationToken);
                return;
            }

            // Another connection of the same person has added the row between the read and the write.
            catch (DbUpdateException) when (attempt == 0)
            {
            }
        }
    }
}
