using Microsoft.EntityFrameworkCore;

namespace Core.DatabaseSystem.Guilds
{
    public sealed class GuildContext : Context
    {
        public DbSet<GuildModel> Guilds { set; get; }
    }
}