using Microsoft.EntityFrameworkCore;

namespace Core.DatabaseSystem.Storages
{
    public sealed class ItemContext : Context
    {
        public DbSet<ItemModel> Items { set; get; }
    }
}