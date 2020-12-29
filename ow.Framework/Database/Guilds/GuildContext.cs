using Microsoft.EntityFrameworkCore;

namespace ow.Framework.Database.Guilds
{
    public sealed class GuildContext : BaseDbContext
    {
        public DbSet<GuildModel> Guilds { set; get; } = default!;

        public GuildContext(DbContextOptions<GuildContext> options) : base(options)
        {
        }
    }
}