using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenWorker.Domain.Persistent;
using OpenWorker.Persistence.Utils;

namespace OpenWorker.Persistence.Configurations;

public sealed class RankingEntryConfiguration : IEntityTypeConfiguration<RankingEntryPersistent>
{
    public void Configure(EntityTypeBuilder<RankingEntryPersistent> builder)
    {
        BasicConfiguration.Configure(builder);

        builder
            .HasOne<PersonPersistent>()
            .WithMany()
            .HasForeignKey(x => x.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.Kind, x.Scope, x.PersonId }).IsUnique();
    }
}
