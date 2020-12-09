using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ow.Framework.Database.Characters
{
    public sealed class CharacterMap : IEntityTypeConfiguration<CharacterModel>
    {
        public void Configure(EntityTypeBuilder<CharacterModel> builder)
        {
        }
    }
}
