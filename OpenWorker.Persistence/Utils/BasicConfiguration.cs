using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenWorker.Domain.Persistent;

namespace OpenWorker.Persistence.Utils;

public static class BasicConfiguration
{
    public static void Configure<TEntity>(EntityTypeBuilder<TEntity> builder) where TEntity : BasicPersistent
    {
        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .ValueGeneratedOnAdd();
        
        builder.Property(x => x.UpdatedAt)
            .IsRequired()
            .ValueGeneratedOnUpdate();
    }
}