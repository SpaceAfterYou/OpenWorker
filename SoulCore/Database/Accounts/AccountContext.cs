using Microsoft.EntityFrameworkCore;

namespace SoulCore.Database.Accounts
{
    public sealed class AccountContext : BaseDbContext
    {
        public DbSet<AccountModel> Accounts { set; get; } = default!;

        public AccountContext(DbContextOptions<AccountContext> context) : base(context)
        {
        }
    }
}
