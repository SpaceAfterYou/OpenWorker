using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenWorker.Domain.Persistent;
using OpenWorker.Persistence.Utils;

namespace OpenWorker.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<AccountPersistent>
{
    public void Configure(EntityTypeBuilder<AccountPersistent> builder)
    {
        BasicConfiguration.Configure(builder);

        builder.Property(x => x.Username)
            .HasMaxLength(12)
            .IsRequired()
            .IsUnicode();

        // Two sign-ups with one name at the same moment both see it as free.
        builder
            .HasIndex(x => x.Username)
            .IsUnique();

        builder.Property(x => x.PasswordHash)
            .IsRequired()
            .HasMaxLength(PasswordHash.PasswordHashSize);

        builder.Property(x => x.SaltHash)
            .IsRequired()
            .HasMaxLength(PasswordHash.PasswordSaltSize);

        // A purchase takes the balance only while enough is left.
        builder.ToTable(table => table.HasCheckConstraint("CK_Accounts_Cash", "\"Cash\" >= 0"));

        builder.HasMany(x => x.Persons)
            .WithOne(x => x.Account)
            .OnDelete(DeleteBehavior.Cascade);
    }
}