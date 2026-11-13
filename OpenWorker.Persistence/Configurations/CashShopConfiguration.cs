using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenWorker.Domain.Persistent;

namespace OpenWorker.Persistence.Configurations;

public sealed class CashShopPurchaseConfiguration : IEntityTypeConfiguration<CashShopPurchasePersistent>
{
    public void Configure(EntityTypeBuilder<CashShopPurchasePersistent> builder)
    {
        // One count per buyer, product and period: the purchase adds to it in one statement that stops at the limit.
        builder.HasKey(x => new { x.AccountId, x.PersonId, x.Product, x.Period });

        builder
            .HasOne<AccountPersistent>()
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable(table => table.HasCheckConstraint("CK_CashShopPurchases_Count", "\"Count\" > 0"));
    }
}

public sealed class CashShopSetConfiguration : IEntityTypeConfiguration<CashShopSetPersistent>
{
    public void Configure(EntityTypeBuilder<CashShopSetPersistent> builder)
    {
        builder.HasKey(x => new { x.PersonId, x.SetNo });

        builder.Property(x => x.Name)
            .HasMaxLength(CashShopSetPersistent.MaxNameLength)
            .IsRequired()
            .IsUnicode();

        builder
            .HasOne<PersonPersistent>()
            .WithMany()
            .HasForeignKey(x => x.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable(table => table.HasCheckConstraint("CK_CashShopSets_SetNo", "\"SetNo\" <= 8"));
    }
}
