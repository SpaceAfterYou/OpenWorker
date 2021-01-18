using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SoulCore.Database.Guilds
{
    public sealed class GuildMap : IEntityTypeConfiguration<GuildModel>
    {
        public void Configure(EntityTypeBuilder<GuildModel> builder)
        {
        }
    }
}
