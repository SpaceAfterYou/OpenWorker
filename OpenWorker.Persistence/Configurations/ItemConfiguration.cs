using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenWorker.Domain.Persistent;
using OpenWorker.Persistence.Utils;

namespace OpenWorker.Persistence.Configurations;

public sealed class ItemConfiguration : IEntityTypeConfiguration<ItemPersistent>
{
    public void Configure(EntityTypeBuilder<ItemPersistent> builder)
    {
        BasicConfiguration.Configure(builder);

        builder.HasKey(x => x.Id);

        // Serials come in blocks from a sequence, so servers never hand out the same one twice.
        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.BroachState)
            .HasMaxLength(15)
            .HasDefaultValue("000000000000000")
            .IsRequired();

        builder.HasOne(x => x.Person)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        // Not unique: two items swapping places are saved one statement after the other.
        builder.HasIndex(x => new { x.PersonId, x.Storage, x.Slot });
    }
}
