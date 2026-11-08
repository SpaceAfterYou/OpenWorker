using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace OpenWorker.Extensions;

/// <summary>
/// Where the database is, in <c>ConnectionStrings:Postgres</c> (or <c>ConnectionStrings__Postgres</c> in the environment),
/// and how it logs, in the <c>Persistence</c> section.
/// </summary>
public sealed class PersistenceOptions
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
}

[OptionsValidator]
internal sealed partial class PersistenceOptionsValidator : IValidateOptions<PersistenceOptions>;
