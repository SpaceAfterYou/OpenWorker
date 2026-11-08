using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OpenWorker.Persistence;

/// <summary>
/// Creates the context for the EF Core tools (<c>dotnet ef migrations add</c>). A server does not use it: it takes the
/// connection string from <c>ConnectionStrings:Postgres</c> (see <c>PersistenceOptions</c>).
/// </summary>
public sealed class PersistenceContextFactory : IDesignTimeDbContextFactory<PersistenceContext>
{
    /// <summary>
    /// The environment variable the tools read, the same one that sets <c>ConnectionStrings:Postgres</c> of a server.
    /// </summary>
    public const string ConnectionVariable = "ConnectionStrings__Postgres";

    /// <summary>
    /// Scaffolding a migration does not connect, so any valid string does when the variable is not set;
    /// applying one (<c>dotnet ef database update</c>) needs the variable.
    /// </summary>
    private const string DesignTimeConnection = "Host=localhost;Database=openworker;Username=postgres";

    public PersistenceContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable(ConnectionVariable);

        var optionsBuilder = new DbContextOptionsBuilder<PersistenceContext>();
        optionsBuilder.UseNpgsql(string.IsNullOrEmpty(connection) ? DesignTimeConnection : connection);

        return new PersistenceContext(optionsBuilder.Options);
    }
}
