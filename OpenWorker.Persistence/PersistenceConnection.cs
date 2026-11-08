using Microsoft.Extensions.Configuration;

namespace OpenWorker.Persistence;

/// <summary>
/// Where the database lives: <c>ConnectionStrings:Postgres</c> (or <c>ConnectionStrings__Postgres</c>
/// in the environment), and the database of <c>compose.yaml</c> when nothing is set.
/// </summary>
public static class PersistenceConnection
{
    public const string Name = "Postgres";
    public const string Default = "Host=postgres;Port=5432;Database=openworker;Username=postgres";

    public static string Resolve(IConfiguration configuration)
    {
        return configuration.GetConnectionString(Name) is { Length: > 0 } value ? value : Default;
    }

    /// <summary>
    /// For tools that run without a host: migrations at design time and UpdateContent.
    /// </summary>
    public static string FromEnvironment()
    {
        return Environment.GetEnvironmentVariable($"ConnectionStrings__{Name}") is { Length: > 0 } value ? value : Default;
    }
}
