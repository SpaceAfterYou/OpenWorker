using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenWorker.Domain.Persistent;
using OpenWorker.Persistence.Utils;

namespace OpenWorker.Persistence.Configurations;

public sealed class LeagueConfiguration : IEntityTypeConfiguration<LeaguePersistent>
{
    public void Configure(EntityTypeBuilder<LeaguePersistent> builder)
    {
        BasicConfiguration.Configure(builder);

        builder.Property(x => x.Name)
            .HasMaxLength(12)
            .IsRequired()
            .IsUnicode();
        //
        // builder
        //     .HasOne(x => x.Master)
        //     .WithOne(x => x.League);
    }
}