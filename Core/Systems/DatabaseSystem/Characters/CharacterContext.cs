using Microsoft.EntityFrameworkCore;

namespace Core.DatabaseSystem.Characters
{
    public sealed class CharacterContext : Context
    {
        public DbSet<CharacterModel> Character { set; get; }
    }
}