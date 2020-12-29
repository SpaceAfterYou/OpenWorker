using Microsoft.EntityFrameworkCore;

namespace ow.Framework.Database.Accounts
{
    public sealed class AccountContext : BaseDbContext
    {
        public DbSet<AccountModel> Accounts { set; get; } = default!;

        private AccountContext()
        {
        }
    }
}