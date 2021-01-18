using Microsoft.EntityFrameworkCore;

namespace SoulCore.Database.AccouintPosts
{
    public sealed class AccountPostContext : BaseDbContext
    {
        public DbSet<AccountPostModel> AccountsPosts { set; get; } = default!;

        public AccountPostContext(DbContextOptions<AccountPostContext> options) : base(options)
        {
        }
    }
}
