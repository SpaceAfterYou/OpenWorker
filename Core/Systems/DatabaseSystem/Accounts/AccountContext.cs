using Microsoft.EntityFrameworkCore;

namespace Core.DatabaseSystem.Accounts
{
    public sealed class AccountContext : Context
    {
        public DbSet<AccountModel> Account { set; get; }
    }
}