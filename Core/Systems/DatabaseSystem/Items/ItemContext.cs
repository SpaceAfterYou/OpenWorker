using Microsoft.EntityFrameworkCore;

namespace Core.Systems.DatabaseSystem.Storages
{
    public sealed class ItemContext : Context
    {
        public DbSet<ItemModel> Items { set; get; }
    }
}