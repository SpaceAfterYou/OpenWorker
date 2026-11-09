using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenWorker.Domain.Persistent;

namespace OpenWorker.Persistence.Configurations;

/// <summary>
/// One row per content of the game, keyed by its index. The key was an attribute on the type; it lives here so that
/// <c>OpenWorker.Domain</c> carries no package of EF Core.
/// </summary>
public sealed class ServerContentConfiguration : IEntityTypeConfiguration<ServerContentPersistent>
{
    public void Configure(EntityTypeBuilder<ServerContentPersistent> builder)
    {
        builder.HasKey(x => x.Index);
    }
}
