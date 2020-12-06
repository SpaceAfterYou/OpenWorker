using Microsoft.EntityFrameworkCore;

namespace Core.Systems.DatabaseSystem.Characters
{
    public sealed class CharacterContext : Context
    {
        public DbSet<CharacterModel> Characters { set; get; }
    }
}