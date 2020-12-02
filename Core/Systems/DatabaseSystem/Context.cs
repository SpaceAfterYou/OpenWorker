using Microsoft.EntityFrameworkCore;

namespace Core.DatabaseSystem
{
    public class Context : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=soulworker;Username=postgres;Password=postgres");
            }
        }
    }
}