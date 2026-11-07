using System.Reflection;
using Microsoft.EntityFrameworkCore;
using OpenWorker.Domain.Persistent;

namespace OpenWorker.Persistence;

public sealed class PersistenceContext(DbContextOptions<PersistenceContext> options) : DbContext(options)
{
    public DbSet<AccountPersistent> Accounts { get; init; }
    public DbSet<FriendPersistent> Friends { get; init; }
    public DbSet<GatePersistent> Gates { get; init; }
    public DbSet<LeagueApplicantPersistent> LeagueApplicants { get; init; }
    public DbSet<LeagueBoardPersistent> LeagueBoards { get; init; }
    public DbSet<LeagueMemberPersistent> LeagueMembers { get; init; }
    public DbSet<LeaguePersistent> Leagues { get; init; }
    public DbSet<PersonPersistent> Persons { get; init; }
    public DbSet<PersonStatePersistent> PersonStates { get; init; }
    public DbSet<ServerContentPersistent> ServerContents { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}