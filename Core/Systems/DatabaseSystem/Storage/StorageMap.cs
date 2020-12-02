using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.DatabaseSystem.Storages
{
    public sealed class StorageMap : IEntityTypeConfiguration<StorageModel>
    {
        public void Configure(EntityTypeBuilder<StorageModel> builder)
        {
        }
    }
}