using Microsoft.EntityFrameworkCore;

namespace Core.DatabaseSystem.AccouintPosts
{
    public sealed class AccountPostContext : Context
    {
        public DbSet<AccountPostModel> Post { set; get; }
    }
}