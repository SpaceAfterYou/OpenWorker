using Microsoft.EntityFrameworkCore;
using System;

namespace Core.Systems.DatabaseSystem
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

        public int UseAndSave(Action<Context> action)
        {
            action(this);
            return SaveChanges();
        }
    }
}