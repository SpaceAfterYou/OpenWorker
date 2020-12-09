using Microsoft.EntityFrameworkCore;

namespace ow.Framework.Database.Characters
{
    public sealed class CharacterContext : BaseDbContext
    {
        public DbSet<CharacterModel> Characters { set; get; }
    }
}
