using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenWorker.Domain.Persistent;
using OpenWorker.Persistence.Utils;

namespace OpenWorker.Persistence.Configurations;

public sealed class MyRoomConfiguration : IEntityTypeConfiguration<MyRoomPersistent>
{
    /// <summary>
    /// The longest name the client lets a room have.
    /// </summary>
    public const int NameLength = 20;

    public void Configure(EntityTypeBuilder<MyRoomPersistent> builder)
    {
        BasicConfiguration.Configure(builder);

        // One room per account and gate.
        builder.HasKey(x => new { x.AccountId, x.GateId });

        builder.Property(x => x.Name)
            .HasMaxLength(NameLength)
            .IsRequired();

        builder
            .HasOne<AccountPersistent>()
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne<GatePersistent>()
            .WithMany()
            .HasForeignKey(x => x.GateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class MyRoomFurnitureConfiguration : IEntityTypeConfiguration<MyRoomFurniturePersistent>
{
    public void Configure(EntityTypeBuilder<MyRoomFurniturePersistent> builder)
    {
        BasicConfiguration.Configure(builder);

        // A serial stands in one room once.
        builder.HasKey(x => x.Serial);

        builder.Property(x => x.Serial)
            .ValueGeneratedNever();

        builder
            .HasOne<MyRoomPersistent>()
            .WithMany()
            .HasForeignKey(x => new { x.AccountId, x.GateId })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.AccountId, x.GateId });
    }
}
