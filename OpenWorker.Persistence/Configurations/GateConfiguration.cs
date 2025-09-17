using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenWorker.Domain.Persistent;
using OpenWorker.Persistence.Utils;

namespace OpenWorker.Persistence.Configurations;

public sealed class GateConfiguration : IEntityTypeConfiguration<GatePersistent>
{
    public void Configure(EntityTypeBuilder<GatePersistent> builder)
    {
        BasicConfiguration.Configure(builder);

        builder.Property(x => x.Name)
            .HasMaxLength(255)
            .IsRequired()
            .IsUnicode();

        builder.HasMany(x => x.Persons)
            .WithOne(x => x.Gate)
            .OnDelete(DeleteBehavior.Cascade);
    }
}