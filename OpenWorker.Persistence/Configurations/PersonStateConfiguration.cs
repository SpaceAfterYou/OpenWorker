using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenWorker.Domain.Persistent;
using OpenWorker.Persistence.Utils;

namespace OpenWorker.Persistence.Configurations;

public sealed class PersonStateConfiguration : IEntityTypeConfiguration<PersonStatePersistent>
{
    public void Configure(EntityTypeBuilder<PersonStatePersistent> builder)
    {
        BasicConfiguration.Configure(builder);

        builder
            .HasKey(x => new { x.PersonId, x.Kind });

        builder
            .Property(x => x.Payload)
            .HasColumnType("jsonb")
            .IsRequired();

        builder
            .HasOne<PersonPersistent>()
            .WithMany()
            .HasForeignKey(x => x.PersonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
