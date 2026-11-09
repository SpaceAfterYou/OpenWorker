using System.Data.Common;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Persistent;
using OpenWorker.LoadTest.Bots;
using OpenWorker.LoadTest.Cli;
using OpenWorker.Persistence;
using OpenWorker.Persistence.Utils;

namespace OpenWorker.LoadTest.Commands;

/// <summary>
/// What the seed command is told.
/// </summary>
internal sealed class SeedOptions
{
    public string Postgres { get; set; } = "Host=localhost;Port=5432;Database=openworker;Username=postgres";

    public string AccountPrefix { get; set; } = "lt";

    public int AccountOffset { get; set; }

    public int Count { get; set; } = 1000;

    public string Password { get; set; } = "loadtest";

    public TimeSpan Wait { get; set; } = TimeSpan.FromSeconds(60);

    public short Gate { get; set; } = 1;

    public string GateName { get; set; } = "LoadTest";

    public static IReadOnlyList<OptionSpec<SeedOptions>> Specs { get; } =
    [
        Text("postgres", "connection", "Connection string of the database the servers use", "Host=localhost;Port=5432;Database=openworker;Username=postgres", (o, v) => o.Postgres = v),
        Text("account-prefix", "text", "Accounts are this text and a number: lt00000, lt00001, ...", "lt", (o, v) => o.AccountPrefix = v),
        Whole("account-offset", "Number of the first account", 0, 0, 10_000_000, (o, v) => o.AccountOffset = v),
        Whole("count", "How many accounts there should be", 1000, 1, 100_000, (o, v) => o.Count = v),
        Text("password", "text", "Password of the accounts", "loadtest", (o, v) => o.Password = v),
        Whole("gate", "Gate the accounts play on, made when the database has none of this number (Gate:Id of the gate server); 0: none", 1, 0, short.MaxValue, (o, v) => o.Gate = (short)v),
        Text("gate-name", "text", "Name of the gate when it is made", "LoadTest", (o, v) => o.GateName = v),
        new OptionSpec<SeedOptions>
        {
            Name = "wait",
            ValueName = "time",
            Group = "Seeding",
            Description = "How long to wait for the servers to create the tables of the database",
            DefaultText = "1m",
            Apply = (o, v) =>
            {
                var problem = OptionParser.ParseDuration(v, TimeSpan.Zero, TimeSpan.FromHours(1), out var result);

                if (problem is null)
                {
                    o.Wait = result;
                }

                return problem;
            }
        }
    ];

    public IEnumerable<string> Validate()
    {
        if (AccountPrefix.Length == 0 || !AccountPrefix.All(char.IsAsciiLetterOrDigit))
        {
            yield return "--account-prefix must be letters and digits.";
        }

        if (!AccountNames.Fits(AccountPrefix, AccountOffset, Count))
        {
            yield return $"The account names '{AccountPrefix}' + a number do not fit the {AccountNames.MaxLength} characters an account name has.";
        }
    }

    private static OptionSpec<SeedOptions> Text(string name, string valueName, string description, string @default, Action<SeedOptions, string> set)
    {
        return new OptionSpec<SeedOptions>
        {
            Name = name,
            ValueName = valueName,
            Group = "Seeding",
            Description = description,
            DefaultText = @default,
            Apply = (o, v) =>
            {
                set(o, v);

                return null;
            }
        };
    }

    private static OptionSpec<SeedOptions> Whole(string name, string description, int @default, int min, int max, Action<SeedOptions, int> set)
    {
        return new OptionSpec<SeedOptions>
        {
            Name = name,
            ValueName = "n",
            Group = "Seeding",
            Description = description,
            DefaultText = @default.ToString(CultureInfo.InvariantCulture),
            Apply = (o, v) =>
            {
                var problem = OptionParser.ParseInt(v, min, max, out var result);

                if (problem is null)
                {
                    set(o, result);
                }

                return problem;
            }
        };
    }
}

/// <summary>
/// Creates the accounts the bots sign in with. A server makes an account only on a request in Discord, so a run of
/// a thousand bots needs them put into the database first. The command writes to the database directly and changes
/// nothing in the servers: it makes the same rows the Discord command does, with the same password hash, and
/// leaves alone the accounts that exist. The same holds for the gate: the auth server lists only the gates of the
/// database, which also come from a Discord command, so a fresh database gets the one the gate server registers as.
/// Characters are not made here; the bot creates one with the protocol, the
/// way a player does, when its account has none.
/// </summary>
internal static class SeedCommand
{
    public static async Task<int> RunAsync(SeedOptions options, TextWriter output, CancellationToken cancellation)
    {
        var builder = new DbContextOptionsBuilder<PersistenceContext>().UseNpgsql(options.Postgres);

        await using var database = new PersistenceContext(builder.Options);

        var existing = await ReadExistingAsync(database, options, output, cancellation);

        if (existing is null)
        {
            return 1;
        }

        // One salt and one hash serve every account: the server checks each login on its own, and hashing a
        // thousand times would only make the command slow.
        PasswordHash.Create(options.Password, out var hash, out var salt);

        var created = 0;

        for (var i = 0; i < options.Count; i++)
        {
            var name = AccountNames.Format(options.AccountPrefix, options.AccountOffset + i);

            if (existing.Contains(name))
            {
                continue;
            }

            database.Accounts.Add(new AccountPersistent { Username = name, PasswordHash = hash, SaltHash = salt });

            created++;
        }

        await database.SaveChangesAsync(cancellation);

        output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"{created} accounts created, {options.Count - created} were there already: {AccountNames.Format(options.AccountPrefix, options.AccountOffset)} to {AccountNames.Format(options.AccountPrefix, options.AccountOffset + options.Count - 1)}."));

        if (options.Gate > 0)
        {
            await SeedGateAsync(database, options, output, cancellation);
        }

        return 0;
    }

    /// <summary>
    /// Makes the gate the gate server registers as (its Gate:Id), unless the database has it. Without the row the
    /// auth server lists no gate and every bot stops at the gate list. The number is given, not generated, so the
    /// identity of the table is moved past it for the gates that a Discord command makes later.
    /// </summary>
    private static async Task SeedGateAsync(PersistenceContext database, SeedOptions options, TextWriter output, CancellationToken cancellation)
    {
        if (await database.Gates.AnyAsync(e => e.Id == options.Gate, cancellation))
        {
            output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Gate {options.Gate} was there already."));

            return;
        }

        database.Gates.Add(new GatePersistent { Id = options.Gate, Name = options.GateName });

        await database.SaveChangesAsync(cancellation);

        await database.Database.ExecuteSqlRawAsync(
            """SELECT setval(pg_get_serial_sequence('"Gates"', 'Id'), (SELECT max("Id") FROM "Gates"))""",
            cancellation);

        output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Gate {options.Gate} created: {options.GateName}."));
    }

    /// <summary>
    /// The names the database already has under the prefix. The servers create the tables when they start, so a
    /// database that does not have them yet is asked again until the time to wait is over.
    /// </summary>
    private static async Task<HashSet<string>?> ReadExistingAsync(PersistenceContext database, SeedOptions options, TextWriter output, CancellationToken cancellation)
    {
        var deadline = DateTime.UtcNow + options.Wait;

        while (true)
        {
            try
            {
                var names = await database.Accounts
                    .Where(e => e.Username.StartsWith(options.AccountPrefix))
                    .Select(e => e.Username)
                    .ToListAsync(cancellation);

                return [.. names];
            }

            catch (Exception e) when (e is DbException or InvalidOperationException && !cancellation.IsCancellationRequested)
            {
                if (DateTime.UtcNow >= deadline)
                {
                    output.WriteLine($"The database is not ready: {e.GetBaseException().Message}");

                    return null;
                }

                await Task.Delay(TimeSpan.FromSeconds(2), cancellation);
            }
        }
    }
}
