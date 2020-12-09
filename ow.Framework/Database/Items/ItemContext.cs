using Microsoft.EntityFrameworkCore;

namespace ow.Framework.Database.Storages
{
    public sealed class ItemContext : BaseDbContext
    {
        public DbSet<ItemModel> Items { set; get; }
    }
}
