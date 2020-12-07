using Microsoft.EntityFrameworkCore;

namespace Core.Systems.DatabaseSystem
{
    public class Context : DbContext
    {
        public Context(DbContextOptions<Context> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=soulworker;Username=postgres;Password=postgres");
            }
        }
    }
}