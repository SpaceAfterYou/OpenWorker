using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenWorker.Domain.Persistent;
using OpenWorker.Persistence.Utils;

namespace OpenWorker.Persistence.Configurations;

public sealed class LeagueConfiguration : IEntityTypeConfiguration<LeaguePersistent>
{
    public void Configure(EntityTypeBuilder<LeaguePersistent> builder)
    {
        BasicConfiguration.Configure(builder);

        builder.Property(x => x.Name)
            .HasMaxLength(8)
            .IsRequired()
            .IsUnicode();

        builder.Property(x => x.Notice)
            .HasMaxLength(800)
            .IsRequired()
            .IsUnicode();

        builder.Property(x => x.RecruitNotice)
            .HasMaxLength(50)
            .IsRequired()
            .IsUnicode();

        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.MasterId);
    }
}

public sealed class LeagueMemberConfiguration : IEntityTypeConfiguration<LeagueMemberPersistent>
{
    public void Configure(EntityTypeBuilder<LeagueMemberPersistent> builder)
    {
        BasicConfiguration.Configure(builder);

        builder.HasKey(x => x.PersonId);

        builder
            .HasOne<PersonPersistent>()
            .WithOne()
            .HasForeignKey<LeagueMemberPersistent>(x => x.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne<LeaguePersistent>()
            .WithMany()
            .HasForeignKey(x => x.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.LeagueId);
    }
}

public sealed class LeagueApplicantConfiguration : IEntityTypeConfiguration<LeagueApplicantPersistent>
{
    public void Configure(EntityTypeBuilder<LeagueApplicantPersistent> builder)
    {
        BasicConfiguration.Configure(builder);

        builder
            .HasOne<PersonPersistent>()
            .WithMany()
            .HasForeignKey(x => x.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne<LeaguePersistent>()
            .WithMany()
            .HasForeignKey(x => x.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);

        // A person asks a league once.
        builder.HasIndex(x => new { x.LeagueId, x.PersonId }).IsUnique();
        builder.HasIndex(x => x.PersonId);
    }
}

public sealed class LeagueBoardConfiguration : IEntityTypeConfiguration<LeagueBoardPersistent>
{
    public void Configure(EntityTypeBuilder<LeagueBoardPersistent> builder)
    {
        BasicConfiguration.Configure(builder);

        builder.Property(x => x.Message)
            .HasMaxLength(60)
            .IsRequired()
            .IsUnicode();

        builder
            .HasOne<PersonPersistent>()
            .WithMany()
            .HasForeignKey(x => x.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne<LeaguePersistent>()
            .WithMany()
            .HasForeignKey(x => x.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.LeagueId);
    }
}
