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

        builder.Property(x => x.PasswordHash)
            .IsRequired()
            .HasMaxLength(PasswordHash.PasswordHashSize);

        builder.Property(x => x.SaltHash)
            .IsRequired()
            .HasMaxLength(PasswordHash.PasswordSaltSize);

        builder.HasMany(x => x.Persons)
            .WithOne(x => x.Account)
            .OnDelete(DeleteBehavior.Cascade);
    }
}