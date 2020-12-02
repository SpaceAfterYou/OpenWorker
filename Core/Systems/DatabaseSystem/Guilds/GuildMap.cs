using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.DatabaseSystem.Guilds
{
    public sealed class GuildMap : IEntityTypeConfiguration<GuildModel>
    {
        public void Configure(EntityTypeBuilder<GuildModel> builder)
        {
        }
    }
}