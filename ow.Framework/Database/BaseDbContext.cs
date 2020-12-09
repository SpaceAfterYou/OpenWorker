using Microsoft.EntityFrameworkCore;
using System;

namespace ow.Framework.Database
{
    public class BaseDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=soulworker;Username=postgres;Password=postgres");
            }
        }

        public int UseAndSave(Action<BaseDbContext> action)
        {
            action(this);
            return SaveChanges();
        }
    }
}
