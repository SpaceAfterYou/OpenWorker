using Microsoft.EntityFrameworkCore;

namespace SoulCore.Database.Characters
{
    public sealed class CharacterContext : BaseDbContext
    {
        public DbSet<CharacterModel> Characters { set; get; } = default!;

        public CharacterContext(DbContextOptions<CharacterContext> options) : base(options)
        {
        }
    }
}
