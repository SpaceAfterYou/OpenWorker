using Microsoft.EntityFrameworkCore;

namespace Core.DatabaseSystem.CharacterPosts
{
    public sealed class CharacterPostContext : Context
    {
        public DbSet<CharacterPostModel> CharacterPosts { set; get; }
    }
}