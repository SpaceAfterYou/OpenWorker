using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ow.Framework.Database.CharacterPosts
{
    public sealed class CharacterPostMap : IEntityTypeConfiguration<CharacterPostModel>
    {
        public void Configure(EntityTypeBuilder<CharacterPostModel> builder)
        {
        }
    }
}
