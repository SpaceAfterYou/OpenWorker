using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace OpenWorker.Extensions;

/// <summary>
/// Where the database is, in <c>ConnectionStrings:Postgres</c> (or <c>ConnectionStrings__Postgres</c> in the environment),
/// and how it logs, in the <c>Persistence</c> section.
/// </summary>
public sealed class PersistenceOptions : IValidatableObject
{
    public const string SectionName = "Persistence";

    /// <summary>
    /// The name under <c>ConnectionStrings</c>.
    /// </summary>
    public const string ConnectionName = "Postgres";

    [Required(AllowEmptyStrings = false, ErrorMessage = "ConnectionStrings:Postgres must be set to the connection string of the database, for example Host=postgres;Database=openworker;Username=postgres.")]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Writes the values of queries to the log. On in Debug builds, off in Release builds, unless the setting says otherwise.
    /// </summary>
#if DEBUG
    public bool SensitiveDataLogging { get; set; } = true;
#else
    public bool SensitiveDataLogging { get; set; }
#endif

    /// <summary>
    /// This server brings the schema of the database up to date when it starts (<c>Persistence:Migrate</c>). One server of
    /// an installation has it, the relay; every other server waits until the schema is there.
    /// </summary>
    public bool Migrate { get; set; }

    public static TimeSpan MinFlushInterval { get; } = TimeSpan.FromSeconds(1);
    public static TimeSpan MaxFlushInterval { get; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// How often a game server writes what its players changed (<c>Persistence:FlushInterval</c>, <c>00:00:30</c> by
    /// default). A crash of the process loses at most this much; a logout, a move to another server and a stop write at once.
    /// </summary>
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(30);

    public static TimeSpan MinKeepTimeout { get; } = TimeSpan.FromSeconds(1);
    public static TimeSpan MaxKeepTimeout { get; } = TimeSpan.FromHours(1);

    /// <summary>
    /// How long what a player changed is still tried when the database did not take it before the player left
    /// (<c>Persistence:KeepTimeout</c>, <c>00:10:00</c> by default). Until then the session stays held and nobody loads the
    /// person; after it the changes are lost, with an error that names them, and the session is let go.
    /// </summary>
    public TimeSpan KeepTimeout { get; set; } = TimeSpan.FromMinutes(10);

    public const int MaxKeepLimit = 100_000;

    /// <summary>
    /// How many players who left a server keeps at most while their changes wait for the database
    /// (<c>Persistence:KeepLimit</c>, 2000 by default; 0 keeps none). The changes of one more are lost at once, with an error.
    /// </summary>
    public int KeepLimit { get; set; } = 2000;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FlushInterval < MinFlushInterval || FlushInterval > MaxFlushInterval)
        {
            yield return new ValidationResult(
                "Persistence:FlushInterval must be a time from 00:00:01 to 00:05:00, for example 00:00:30.", [nameof(FlushInterval)]);
        }

        if (KeepTimeout < MinKeepTimeout || KeepTimeout > MaxKeepTimeout)
        {
            yield return new ValidationResult(
                "Persistence:KeepTimeout must be a time from 00:00:01 to 01:00:00, for example 00:10:00.", [nameof(KeepTimeout)]);
        }

        if (KeepLimit is < 0 or > MaxKeepLimit)
        {
            yield return new ValidationResult(
                "Persistence:KeepLimit must be a number from 0 to 100000, for example 2000.", [nameof(KeepLimit)]);
        }
    }
}

[OptionsValidator]
internal sealed partial class PersistenceOptionsValidator : IValidateOptions<PersistenceOptions>;
