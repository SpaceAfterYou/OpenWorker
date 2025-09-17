using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OpenWorker.Persistence;

public sealed class PersistenceContextFactory : IDesignTimeDbContextFactory<PersistenceContext>
{
    public PersistenceContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PersistenceContext>();
        optionsBuilder.UseNpgsql("Host=postgres;Port=5432;Database=openworker;Username=postgres");

        return new PersistenceContext(optionsBuilder.Options);
    }
}