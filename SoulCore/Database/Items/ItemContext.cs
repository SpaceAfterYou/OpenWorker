using Microsoft.EntityFrameworkCore;

namespace SoulCore.Database.Storages
{
    public sealed class ItemContext : BaseDbContext
    {
        public DbSet<ItemModel> Items { set; get; } = default!;

        public ItemContext(DbContextOptions<ItemContext> options) : base(options)
        {
        }
    }
}
