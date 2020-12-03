using Microsoft.EntityFrameworkCore;

namespace Core.DatabaseSystem.AccouintPosts
{
    public sealed class AccountPostContext : Context
    {
        public DbSet<AccountPostModel> AccountsPosts { set; get; }
    }
}