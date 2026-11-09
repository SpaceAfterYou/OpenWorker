using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenWorker.Domain.Persistent;
using OpenWorker.Persistence.Utils;

namespace OpenWorker.Persistence.Configurations;

public sealed class PostConfiguration : IEntityTypeConfiguration<PostPersistent>
{
    public void Configure(EntityTypeBuilder<PostPersistent> builder)
    {
        BasicConfiguration.Configure(builder);

        builder.Property(x => x.SenderName)
            .HasMaxLength(PostPersistent.MaxNameLength)
            .IsRequired()
            .IsUnicode();

        builder.Property(x => x.ReceiverName)
            .HasMaxLength(PostPersistent.MaxNameLength)
            .IsRequired()
            .IsUnicode();

        builder.Property(x => x.Title)
            .HasMaxLength(PostPersistent.MaxTitleLength)
            .IsRequired()
            .IsUnicode();

        builder.Property(x => x.Body)
            .HasMaxLength(PostPersistent.MaxBodyLength)
            .IsRequired()
            .IsUnicode();

        // What the letter carries travels with it, never as items of anybody.
        builder.OwnsMany(x => x.Items, items => items.ToJson());

        // The sender may go; the letter stays with the receiver under the name it was sent with.
        builder
            .HasOne<PersonPersistent>()
            .WithMany()
            .HasForeignKey(x => x.SenderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasOne<PersonPersistent>()
            .WithMany()
            .HasForeignKey(x => x.ReceiverId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne<AccountPersistent>()
            .WithMany()
            .HasForeignKey(x => x.ReceiverAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        // The box of a person, the sent letters of a person and the box of an account.
        builder.HasIndex(x => x.ReceiverId);
        builder.HasIndex(x => x.SenderId);
        builder.HasIndex(x => x.ReceiverAccountId);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_Posts_Money", "\"Money\" >= 0");
            table.HasCheckConstraint("CK_Posts_Postage", "\"Postage\" >= 0");

            // A letter is for a person or for an account, as its kind says.
            table.HasCheckConstraint("CK_Posts_Receiver",
                "(\"Kind\" = 0 AND \"ReceiverId\" IS NOT NULL AND \"ReceiverAccountId\" IS NULL) OR " +
                "(\"Kind\" = 1 AND \"ReceiverAccountId\" IS NOT NULL AND \"ReceiverId\" IS NULL)");
        });
    }
}
