using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ow.Framework.Database.Storages
{
    public sealed class ItemMap : IEntityTypeConfiguration<ItemModel>
    {
        public void Configure(EntityTypeBuilder<ItemModel> builder)
        {
        }
    }
}
