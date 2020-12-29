using Microsoft.EntityFrameworkCore;

namespace ow.Framework.Database.Characters
{
    public sealed class CharacterContext : BaseDbContext
    {
        public DbSet<CharacterModel> Characters { set; get; } = default!;

        public CharacterContext(DbContextOptions<CharacterContext> options) : base(options)
        {
        }
    }
}