using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ow.Framework.Database.Guilds
{
    public sealed class GuildMap : IEntityTypeConfiguration<GuildModel>
    {
        public void Configure(EntityTypeBuilder<GuildModel> builder)
        {
        }
    }
}
