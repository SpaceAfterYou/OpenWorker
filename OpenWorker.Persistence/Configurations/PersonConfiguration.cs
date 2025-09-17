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

        builder
            .HasOne(x => x.League)
            .WithMany(x => x.MemberList);
    }
}