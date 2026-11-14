using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenWorker.Domain.Persistent;
using OpenWorker.Persistence.Utils;

namespace OpenWorker.Persistence.Configurations;

public sealed class RankingPeriodConfiguration : IEntityTypeConfiguration<RankingPeriodPersistent>
{
    public void Configure(EntityTypeBuilder<RankingPeriodPersistent> builder)
    {
        BasicConfiguration.Configure(builder);

        // Two servers that end the same period: the second one gives nothing.
        builder.HasIndex(x => new { x.Kind, x.Scope, x.EndedAt }).IsUnique();
    }
}
