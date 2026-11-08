using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using OpenWorker.DependencyInjection;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Hotspot;
using OpenWorker.Persistence;

namespace OpenWorker.AuthServer.Test.DependencyInjection;

/// <summary>
/// The host builds every hosted service before it starts the first one, and the first one migrates the database. A server
/// that read <see cref="ServerContent"/> while its container was built read it before the migration: on a fresh database
/// the table did not exist yet and the server died. These tests build the container the way a server does and check that
/// the table is read only once the hosted services start, after the migration.
/// </summary>
[TestClass]
public sealed class TestServerContentLoad
{
    /// <summary>
    /// Counts the contexts it makes, so a test can tell whether anything went to the database.
    /// </summary>
    private sealed class CountingFactory : IDbContextFactory<PersistenceContext>
    {
        private readonly DbContextOptions<PersistenceContext> _options = new DbContextOptionsBuilder<PersistenceContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        private int _created;

        public int Created => Volatile.Read(ref _created);

        public PersistenceContext CreateDbContext()
        {
            Interlocked.Increment(ref _created);

            return new PersistenceContext(_options);
        }
    }

    /// <summary>
    /// Stands in for the migration: it goes first, and the rows exist only once it has started. It takes the contents in
    /// its constructor, so the container makes them before any hosted service starts, as a handler that needs them would.
    /// </summary>
    private sealed class StandInMigration(ServerContent contents, IDbContextFactory<PersistenceContext> factory) : IHostedService
    {
        public bool ContentsWereEmptyWhenStarted { get; private set; }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            ContentsWereEmptyWhenStarted = contents.All(e => !e);

            await using var database = await factory.CreateDbContextAsync(cancellationToken);

            database.ServerContents.AddRange(
                new ServerContentPersistent { Index = ServerContentIndex.CashShop, State = true },
                new ServerContentPersistent { Index = ServerContentIndex.SecondPassword, State = true },
                new ServerContentPersistent { Index = ServerContentIndex.Attendance, State = false });

            await database.SaveChangesAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    [TestMethod]
    public void BuildingTheContainerReadsNoDatabase()
    {
        var factory = new CountingFactory();

        using var host = new HostBuilder()
            .ConfigureAppConfiguration(configuration => configuration
                .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "Settings", "AuthServer.json"), optional: false))
            .ConfigureServices((context, services) =>
            {
                services.AddPersistence();
                services.AddSingleton<SimulationClock>();
                services.AddHotspot(context, EntityComponentService.Auth);
                services.Replace(ServiceDescriptor.Singleton<IDbContextFactory<PersistenceContext>>(factory));
            })
            .Build();

        // What the host does before it starts the first service.
        var hosted = host.Services.GetServices<IHostedService>().Select(e => e.GetType().Name).ToList();
        var contents = host.Services.GetRequiredService<ServerContent>();

        Assert.AreEqual(0, factory.Created, "Something read the database while the container was built.");
        Assert.IsTrue(contents.All(e => !e), "The contents were filled before the server started.");

        var migration = hosted.IndexOf("PersistenceBootstrapService");
        var load = hosted.IndexOf(nameof(ServerContentLoadService));
        var listener = hosted.IndexOf(nameof(ServerService));

        var order = string.Join(", ", hosted);

        // The exporter of the metrics goes before the migration, it does not touch the database.
        Assert.IsGreaterThanOrEqualTo(0, migration, "There is no migration: " + order);
        Assert.IsGreaterThan(migration, load, "The contents are not loaded after the migration: " + order);
        Assert.IsLessThan(listener, load, "The contents are not loaded before the listener starts: " + order);
    }

    [TestMethod]
    public async Task TheContentsHoldTheRowsTheMigrationMade()
    {
        await using var harness = await ServerHarness.StartAsync(
            services => services.Insert(0, ServiceDescriptor.Singleton<IHostedService, StandInMigration>()),
            cancellationToken: TestContext.CancellationToken);

        var migration = harness.Host.Services.GetServices<IHostedService>().OfType<StandInMigration>().Single();
        var contents = harness.Host.Services.GetRequiredService<ServerContent>();

        Assert.IsTrue(migration.ContentsWereEmptyWhenStarted, "The contents were read before the migration started.");
        Assert.IsTrue(contents[ServerContentIndex.CashShop]);
        Assert.IsTrue(contents[ServerContentIndex.SecondPassword]);
        Assert.IsFalse(contents[ServerContentIndex.Attendance]);
        Assert.HasCount(Enum.GetValues<ServerContentIndex>().Length, contents);
    }

    public TestContext TestContext { get; set; } = null!;
}
