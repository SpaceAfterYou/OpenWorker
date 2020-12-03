using Microsoft.EntityFrameworkCore;

namespace Core.DatabaseSystem.Storages
{
    public sealed class StorageContext : Context
    {
        public DbSet<StorageModel> Storage { set; get; }
    }
}