using Microsoft.EntityFrameworkCore;

namespace Core.Systems.DatabaseSystem.AccouintPosts
{
    public sealed class AccountPostContext : Context
    {
        public DbSet<AccountPostModel> AccountsPosts { set; get; }
    }
}