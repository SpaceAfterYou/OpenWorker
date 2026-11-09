using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenWorker.Domain.Persistent;
using OpenWorker.Persistence.Utils;

namespace OpenWorker.Persistence.Configurations;

public sealed class HelperSupportConfiguration : IEntityTypeConfiguration<HelperSupportPersistent>
{
    public void Configure(EntityTypeBuilder<HelperSupportPersistent> builder)
    {
        BasicConfiguration.Configure(builder);

        // One orb offered per person.
        builder.HasKey(x => x.PersonId);

        builder.Property(x => x.PersonId)
            .ValueGeneratedNever();

        builder
            .HasOne<PersonPersistent>()
            .WithMany()
            .HasForeignKey(x => x.PersonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class HelperSupportUseConfiguration : IEntityTypeConfiguration<HelperSupportUsePersistent>
{
    public void Configure(EntityTypeBuilder<HelperSupportUsePersistent> builder)
    {
        BasicConfiguration.Configure(builder);

        builder
            .HasOne<PersonPersistent>()
            .WithMany()
            .HasForeignKey(x => x.SupporterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne<PersonPersistent>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // A friend uses an orb once.
        builder.HasIndex(x => new { x.SupporterId, x.UserId, x.Until }).IsUnique();
        builder.HasIndex(x => x.UserId);
    }
}
