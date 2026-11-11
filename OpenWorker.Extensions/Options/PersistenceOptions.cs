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

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FlushInterval < MinFlushInterval || FlushInterval > MaxFlushInterval)
        {
            yield return new ValidationResult(
                "Persistence:FlushInterval must be a time from 00:00:01 to 00:05:00, for example 00:00:30.", [nameof(FlushInterval)]);
        }
    }
}

[OptionsValidator]
internal sealed partial class PersistenceOptionsValidator : IValidateOptions<PersistenceOptions>;
