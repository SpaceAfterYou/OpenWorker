using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;

namespace OpenWorker.Persistence;

/// <summary>
/// The rooms over the database: one query or one statement for each question, so the answer is what the rows hold now. The
/// keys decide what may happen once: a room per account and gate, a serial in one room.
/// </summary>
public sealed class MyRoomStore(IDbContextFactory<PersistenceContext> factory) : IMyRoomStore
{
    public async Task<MyRoomRecord?> FindAsync(int account, short gate, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        var room = await database.MyRooms
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.AccountId == account && e.GateId == gate, cancellationToken);

        return room is null ? null : new MyRoomRecord(room.AccountId, room.GateId, room.Index, room.Grid, room.OpenLevel, room.Name);
    }

    public async Task<IReadOnlyList<MyRoomPiece>> FurnitureAsync(int account, short gate, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        return await database.MyRoomFurniture
            .AsNoTracking()
            .Where(e => e.AccountId == account && e.GateId == gate)
            .OrderBy(e => e.CreatedAt)
            .ThenBy(e => e.Serial)
            .Select(e => new MyRoomPiece(e.Serial, e.ItemId, e.Rotation, e.Grid))
            .ToListAsync(cancellationToken);
    }

    public async Task<int?> AccountOfAsync(int person, short gate, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        var accounts = await database.Persons
            .AsNoTracking()
            .Where(e => e.Id == person && e.Gate.Id == gate)
            .Select(e => e.Account.Id)
            .Take(1)
            .ToListAsync(cancellationToken);

        return accounts.Count == 0 ? null : accounts[0];
    }

    public async Task<bool> CreateAsync(MyRoomRecord room, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        database.MyRooms.Add(new MyRoomPersistent
        {
            AccountId = room.Account,
            GateId = room.Gate,
            Index = room.Index,
            Grid = room.Grid,
            OpenLevel = room.OpenLevel,
            Name = room.Name
        });

        try
        {
            await database.SaveChangesAsync(cancellationToken);

            return true;
        }

        // The account made its room on another server a moment before.
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return false;
        }
    }

    public async Task SetupAsync(int account, short gate, byte openLevel, string name, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        var now = DateTime.UtcNow;

        await database.MyRooms
            .Where(e => e.AccountId == account && e.GateId == gate)
            .ExecuteUpdateAsync(e => e
                .SetProperty(p => p.OpenLevel, openLevel)
                .SetProperty(p => p.Name, name)
                .SetProperty(p => p.UpdatedAt, now), cancellationToken);
    }

    public async Task<bool> PlaceAsync(int account, short gate, MyRoomPiece piece, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        database.MyRoomFurniture.Add(new MyRoomFurniturePersistent
        {
            Serial = piece.Serial,
            AccountId = account,
            GateId = gate,
            ItemId = piece.Item,
            Rotation = piece.Rotation,
            Grid = piece.Grid
        });

        try
        {
            await database.SaveChangesAsync(cancellationToken);

            return true;
        }

        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return false;
        }
    }

    public async Task<bool> RemoveAsync(int account, short gate, long serial, CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        // Of two servers that take the same piece away, the one whose statement removes the row gives the item back.
        var removed = await database.MyRoomFurniture
            .Where(e => e.Serial == serial && e.AccountId == account && e.GateId == gate)
            .ExecuteDeleteAsync(cancellationToken);

        return removed > 0;
    }

    public async Task MoveAsync(int account, short gate, IReadOnlyCollection<MyRoomPiece> pieces, CancellationToken cancellationToken = default)
    {
        if (pieces.Count == 0)
        {
            return;
        }

        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);

        var now = DateTime.UtcNow;

        foreach (var piece in pieces)
        {
            await database.MyRoomFurniture
                .Where(e => e.Serial == piece.Serial && e.AccountId == account && e.GateId == gate)
                .ExecuteUpdateAsync(e => e
                    .SetProperty(p => p.Rotation, piece.Rotation)
                    .SetProperty(p => p.Grid, piece.Grid)
                    .SetProperty(p => p.UpdatedAt, now), cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
