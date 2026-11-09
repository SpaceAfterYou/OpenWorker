using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;

namespace OpenWorker.Persistence;

public static class ItemStore
{
    /// <summary>
    /// How many statements go into one command. Each is a few parameters; the database takes a command as one
    /// round trip, and a very long one only holds the connection longer.
    /// </summary>
    private const int StatementsPerCommand = 256;

    public static async Task<List<ItemPersistent>> LoadItemsAsync(
        this IDbContextFactory<PersistenceContext> factory,
        int person,
        CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        return await database.Items
            .AsNoTracking()
            .Where(e => e.PersonId == person)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Takes serials for new items. Several servers write to the same table, so the database hands them out.
    /// </summary>
    public static async Task<long[]> ReserveSerialsAsync(
        this IDbContextFactory<PersistenceContext> factory,
        int count,
        CancellationToken cancellationToken = default)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        var query = "SELECT nextval('\"" + PersistenceContext.ItemSerialSequence + "\"') AS \"Value\" FROM generate_series(1, {0})";

        var serials = await database.Database
            .SqlQueryRaw<long>(query, count)
            .ToArrayAsync(cancellationToken);

        return serials;
    }

    public static Task SaveChangesAsync(
        this IDbContextFactory<PersistenceContext> factory,
        PersonChanges changes,
        CancellationToken cancellationToken = default)
    {
        return factory.SaveChangesAsync([changes], cancellationToken);
    }

    /// <summary>
    /// Writes the changes of many persons in one transaction, in the order given: the changes of one person that
    /// appear twice are written oldest first. The statements go to the database a few hundred at a time, each such
    /// command in one round trip.
    /// </summary>
    public static async Task SaveChangesAsync(
        this IDbContextFactory<PersistenceContext> factory,
        IReadOnlyList<PersonChanges> batch,
        CancellationToken cancellationToken = default)
    {
        var commands = Build(batch, DateTime.UtcNow);

        if (commands.Count == 0)
        {
            return;
        }

        await using var database = await factory.CreateDbContextAsync(cancellationToken);

        // One command is atomic by itself; several need the transaction.
        await using var transaction = commands.Count > 1 ? await database.Database.BeginTransactionAsync(cancellationToken) : null;

        foreach (var (sql, parameters) in commands)
        {
            await database.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
        }

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
    }

    /// <summary>
    /// The commands that write the batch. Raw statements rather than the change tracker: a save never reads first, an item
    /// is written whether it is new or not (<c>ON CONFLICT</c>), and a progress document is one upsert.
    /// </summary>
    private static List<(string Sql, List<NpgsqlParameter> Parameters)> Build(IReadOnlyList<PersonChanges> batch, DateTime now)
    {
        var commands = new List<(string, List<NpgsqlParameter>)>();
        var sql = new StringBuilder();
        var parameters = new List<NpgsqlParameter>();
        var statements = 0;

        string Add(object value, NpgsqlDbType? type = null)
        {
            var name = "p" + parameters.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);

            parameters.Add(type is { } dbType
                ? new NpgsqlParameter(name, dbType) { Value = value }
                : new NpgsqlParameter(name, value));

            return "@" + name;
        }

        void End()
        {
            sql.Append(";\n");

            if (++statements < StatementsPerCommand)
            {
                return;
            }

            Flush();
        }

        void Flush()
        {
            if (statements == 0)
            {
                return;
            }

            commands.Add((sql.ToString(), parameters));

            sql.Clear();
            parameters = [];
            statements = 0;
        }

        foreach (var changes in batch)
        {
            if (changes.Removed.Count > 0)
            {
                sql.Append("DELETE FROM \"Items\" WHERE \"PersonId\" = ").Append(Add(changes.Person))
                    .Append(" AND \"Id\" = ANY(").Append(Add(changes.Removed.ToArray())).Append(')');
                End();
            }

            foreach (var item in changes.Added.Concat(changes.Changed))
            {
                AppendItem(sql, item, Add(changes.Person), Add(now), Add);
                End();
            }

            if (changes.State is { } state)
            {
                sql.Append("UPDATE \"Persons\" SET ")
                    .Append("\"Level\" = ").Append(Add((short)state.Level)).Append(", ")
                    .Append("\"Experience\" = ").Append(Add(state.Experience)).Append(", ")
                    .Append("\"Gold\" = ").Append(Add(state.Gold)).Append(", ")
                    .Append("\"BattlePoint\" = ").Append(Add(state.BattlePoint)).Append(", ")
                    .Append("\"Ether\" = ").Append(Add(state.Ether)).Append(", ")
                    .Append("\"Currencies\" = ").Append(Add(state.Currencies)).Append(", ")
                    .Append("\"StorageGrades\" = ").Append(Add(state.StorageGrades, NpgsqlDbType.Bytea)).Append(", ")
                    .Append("\"QuickSlotCards\" = ").Append(Add(state.QuickSlotCards)).Append(", ")
                    .Append("\"QuickSlotItems\" = ").Append(Add(state.QuickSlotItems)).Append(", ")
                    .Append("\"UpdatedAt\" = ").Append(Add(now))
                    .Append(" WHERE \"Id\" = ").Append(Add(changes.Person));
                End();
            }

            if (changes.Place is { } place)
            {
                sql.Append("UPDATE \"Persons\" SET ")
                    .Append("\"Location\" = ").Append(Add(place.Location)).Append(", ")
                    .Append("\"PositionX\" = ").Append(Add(place.X)).Append(", ")
                    .Append("\"PositionY\" = ").Append(Add(place.Y)).Append(", ")
                    .Append("\"PositionZ\" = ").Append(Add(place.Z)).Append(", ")
                    .Append("\"RotationX\" = ").Append(Add(place.Rotation))
                    .Append(" WHERE \"Id\" = ").Append(Add(changes.Person));
                End();
            }

            foreach (var document in changes.Documents)
            {
                AppendDocument(sql, Add(changes.Person), Add((short)document.Kind), Add(document.Payload, NpgsqlDbType.Jsonb), Add(now));
                End();
            }
        }

        Flush();

        return commands;
    }

    private static void AppendItem(StringBuilder sql, ItemPersistent item, string person, string now, Func<object, NpgsqlDbType?, string> add)
    {
        string Add(object? value) => add(value ?? DBNull.Value, null);

        sql.Append("INSERT INTO \"Items\" (\"Id\", \"PersonId\", \"Storage\", \"Slot\", \"Item\", \"Count\", \"BindType\", \"Upgrade\", ")
            .Append("\"Endurance\", \"SocketActive\", \"DueTo\", \"UpgradeCount\", \"UpgradeLimit\", \"Flag\", \"Exp\", \"BroachState\", ")
            .Append("\"RestoreCount\", \"SealCount\", \"OptionType\", \"OptionValue\", \"CreatedAt\", \"UpdatedAt\") VALUES (")
            .Append(Add(item.Id)).Append(", ")
            .Append(person).Append(", ")
            .Append(Add((short)item.Storage)).Append(", ")
            .Append(Add(item.Slot)).Append(", ")
            .Append(Add(item.Item)).Append(", ")
            .Append(Add(item.Count)).Append(", ")
            .Append(Add((short)item.BindType)).Append(", ")
            .Append(Add((short)item.Upgrade)).Append(", ")
            .Append(Add((short)item.Endurance)).Append(", ")
            .Append(Add((short)item.SocketActive)).Append(", ")
            .Append(add(item.DueTo is { } due ? due : DBNull.Value, NpgsqlDbType.TimestampTz)).Append(", ")
            .Append(Add((short)item.UpgradeCount)).Append(", ")
            .Append(Add((short)item.UpgradeLimit)).Append(", ")
            .Append(Add((short)item.Flag)).Append(", ")
            .Append(Add(item.Exp)).Append(", ")
            .Append(Add(item.BroachState)).Append(", ")
            .Append(Add((short)item.RestoreCount)).Append(", ")
            .Append(Add((short)item.SealCount)).Append(", ")
            .Append(Add(item.OptionType)).Append(", ")
            .Append(Add(item.OptionValue)).Append(", ")
            .Append(now).Append(", ")
            .Append(now)
            .Append(") ON CONFLICT (\"Id\") DO UPDATE SET ")
            .Append("\"Storage\" = EXCLUDED.\"Storage\", \"Slot\" = EXCLUDED.\"Slot\", \"Count\" = EXCLUDED.\"Count\", ")
            .Append("\"BindType\" = EXCLUDED.\"BindType\", \"Upgrade\" = EXCLUDED.\"Upgrade\", \"Endurance\" = EXCLUDED.\"Endurance\", ")
            .Append("\"SocketActive\" = EXCLUDED.\"SocketActive\", \"DueTo\" = EXCLUDED.\"DueTo\", ")
            .Append("\"UpgradeCount\" = EXCLUDED.\"UpgradeCount\", \"UpgradeLimit\" = EXCLUDED.\"UpgradeLimit\", ")
            .Append("\"Flag\" = EXCLUDED.\"Flag\", \"Exp\" = EXCLUDED.\"Exp\", \"BroachState\" = EXCLUDED.\"BroachState\", ")
            .Append("\"RestoreCount\" = EXCLUDED.\"RestoreCount\", \"SealCount\" = EXCLUDED.\"SealCount\", ")
            .Append("\"OptionType\" = EXCLUDED.\"OptionType\", \"OptionValue\" = EXCLUDED.\"OptionValue\", ")
            .Append("\"UpdatedAt\" = EXCLUDED.\"UpdatedAt\"");
    }

    /// <summary>
    /// One statement that writes the document whether the row is there or not: no read, no retry.
    /// </summary>
    internal static void AppendDocument(StringBuilder sql, string person, string kind, string payload, string now)
    {
        sql.Append("INSERT INTO \"PersonStates\" (\"PersonId\", \"Kind\", \"Payload\", \"CreatedAt\", \"UpdatedAt\") VALUES (")
            .Append(person).Append(", ").Append(kind).Append(", ").Append(payload).Append(", ").Append(now).Append(", ").Append(now)
            .Append(") ON CONFLICT (\"PersonId\", \"Kind\") DO UPDATE SET \"Payload\" = EXCLUDED.\"Payload\", \"UpdatedAt\" = EXCLUDED.\"UpdatedAt\"");
    }
}
