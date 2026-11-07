using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenWorker.Domain.Persistent;
using OpenWorker.Persistence.Utils;

namespace OpenWorker.Persistence.Configurations;

public sealed class FriendConfiguration : IEntityTypeConfiguration<FriendPersistent>
{
    public void Configure(EntityTypeBuilder<FriendPersistent> builder)
    {
        BasicConfiguration.Configure(builder);

        builder
            .HasOne<PersonPersistent>()
            .WithMany()
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne<PersonPersistent>()
            .WithMany()
            .HasForeignKey(x => x.TargetId)
            .OnDelete(DeleteBehavior.Cascade);

        // A person stands one way to another.
        builder.HasIndex(x => new { x.OwnerId, x.TargetId }).IsUnique();
        builder.HasIndex(x => x.TargetId);
    }
}
