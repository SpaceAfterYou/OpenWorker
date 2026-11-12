using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenWorker.Domain.Persistent;
using OpenWorker.Persistence.Utils;

namespace OpenWorker.Persistence.Configurations;

public sealed class ExchangeListingConfiguration : IEntityTypeConfiguration<ExchangeListingPersistent>
{
    public void Configure(EntityTypeBuilder<ExchangeListingPersistent> builder)
    {
        BasicConfiguration.Configure(builder);

        builder.Property(x => x.SellerName)
            .HasMaxLength(ExchangeListingPersistent.MaxNameLength)
            .IsRequired()
            .IsUnicode();

        // A serial of the sequence of the items: the list of the seller keeps listings and items apart by it.
        builder.Property(x => x.Serial)
            .HasDefaultValueSql($"nextval('\"{PersistenceContext.ItemSerialSequence}\"')");

        builder.HasIndex(x => x.Serial).IsUnique();

        // The item travels with the listing, never as an item of anybody.
        builder.OwnsOne(x => x.Goods, goods => goods.ToJson());

        builder
            .HasOne<PersonPersistent>()
            .WithMany()
            .HasForeignKey(x => x.SellerId)
            .OnDelete(DeleteBehavior.Cascade);

        // The list of a seller, and the search: by the item, or by its kind.
        builder.HasIndex(x => new { x.SellerId, x.State });
        builder.HasIndex(x => new { x.State, x.Item, x.PriceOne });
        builder.HasIndex(x => new { x.State, x.Group, x.SubGroup, x.PriceOne });

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_ExchangeListings_Count", "\"Count\" >= 0 AND \"Count\" <= \"InitialCount\"");
            table.HasCheckConstraint("CK_ExchangeListings_Price", "\"PriceOne\" >= 0");
            table.HasCheckConstraint("CK_ExchangeListings_SaleRate", "\"SaleRate\" <= 100");

            // On sale means a price and an end.
            table.HasCheckConstraint("CK_ExchangeListings_OnSale",
                "\"State\" <> 2 OR (\"PriceOne\" > 0 AND \"ExpiresAt\" IS NOT NULL)");
        });
    }
}

public sealed class ExchangeInterestConfiguration : IEntityTypeConfiguration<ExchangeInterestPersistent>
{
    public void Configure(EntityTypeBuilder<ExchangeInterestPersistent> builder)
    {
        // An item is on the list of a person once.
        builder.HasKey(x => new { x.PersonId, x.Item });

        builder
            .HasOne<PersonPersistent>()
            .WithMany()
            .HasForeignKey(x => x.PersonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ExchangeSaleConfiguration : IEntityTypeConfiguration<ExchangeSalePersistent>
{
    public void Configure(EntityTypeBuilder<ExchangeSalePersistent> builder)
    {
        builder.HasIndex(x => new { x.Item, x.SoldAt });

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_ExchangeSales_Count", "\"Count\" > 0");
            table.HasCheckConstraint("CK_ExchangeSales_Price", "\"PriceOne\" > 0");
        });
    }
}
