using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenWorker.Domain.Persistent;
using OpenWorker.Persistence.Utils;

namespace OpenWorker.Persistence.Configurations;

public sealed class PersonConfiguration : IEntityTypeConfiguration<PersonPersistent>
{
    public void Configure(EntityTypeBuilder<PersonPersistent> builder)
    {
        BasicConfiguration.Configure(builder);

        builder
            .Property(x => x.Name)
            .HasMaxLength(12)
            .IsRequired()
            .IsUnicode();

        // Players find each other by name, and servers that create characters at the same time
        // can both see a name as free.
        builder
            .HasIndex(x => x.Name)
            .IsUnique();

        builder
            .Property(x => x.CommunityComment)
            .HasMaxLength(50)
            .HasDefaultValue(string.Empty)
            .IsRequired()
            .IsUnicode();

        builder
            .Property(x => x.CommunityMemo)
            .HasMaxLength(30)
            .HasDefaultValue(string.Empty)
            .IsRequired()
            .IsUnicode();

    }
}