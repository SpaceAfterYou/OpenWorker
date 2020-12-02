using Microsoft.EntityFrameworkCore;

namespace Core.DatabaseSystem.CharacterPosts
{
    public sealed class CharacterPostContext : Context
    {
        public DbSet<CharacterPostModel> Post { set; get; }
    }
}