using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Systems.DatabaseSystem.CharacterPosts
{
    public sealed class CharacterPostMap : IEntityTypeConfiguration<CharacterPostModel>
    {
        public void Configure(EntityTypeBuilder<CharacterPostModel> builder)
        {
        }
    }
}