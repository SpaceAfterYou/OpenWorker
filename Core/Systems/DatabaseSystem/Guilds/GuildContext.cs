using Microsoft.EntityFrameworkCore;

namespace Core.Systems.DatabaseSystem.Guilds
{
    public sealed class GuildContext : Context
    {
        public DbSet<GuildModel> Guilds { set; get; }
    }
}