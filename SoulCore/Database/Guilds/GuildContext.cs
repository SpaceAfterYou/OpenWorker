using Microsoft.EntityFrameworkCore;

namespace SoulCore.Database.Guilds
{
    public sealed class GuildContext : BaseDbContext
    {
        public DbSet<GuildModel> Guilds { set; get; } = default!;

        public GuildContext(DbContextOptions<GuildContext> options) : base(options)
        {
        }
    }
}
