using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SoulCore.Database;
using SoulCore.Database.AccouintPosts;
using SoulCore.Database.Accounts;
using SoulCore.Database.CharacterPosts;
using SoulCore.Database.Characters;
using SoulCore.Database.Guilds;
using SoulCore.Database.Storages;
using SoulCore.Extensions;

namespace SetupDatabase
{
    public sealed class MigrationContext : BaseDbContext
    {
        public DbSet<AccountModel> Account { get; set; }
        public DbSet<CharacterModel> Character { get; set; }
        public DbSet<GuildModel> Guild { get; set; }
        public DbSet<ItemModel> Item { get; set; }
        public DbSet<AccountPostModel> AccountPost { get; set; }
        public DbSet<CharacterPostModel> CharacterPost { get; set; }

        public MigrationContext(DbContextOptions<MigrationContext> options) : base(options)
        {
        }

        //public MigrationContext(DbContextOptions options) : base(options)
        //{
        //}

        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder
            .ApplyConfiguration(new AccountMap())
            .ApplyConfiguration(new CharacterMap())
            .ApplyConfiguration(new GuildMap())
            .ApplyConfiguration(new ItemMap())
            .ApplyConfiguration(new AccountPostMap())
            .ApplyConfiguration(new CharacterPostMap());
    }

    public class Program
    {
        public static void Main(string[] args) =>
            CreateHostBuilder(args).Build().Run();

        public static IHostBuilder CreateHostBuilder(string[] args) => Host
            .CreateDefaultBuilder(args)
            .UseConsoleLifetime()
            .ConfigureAppConfiguration(ConfigureAppConfiguration)
            .ConfigureServices(ConfigureServices);

        public static void ConfigureAppConfiguration(HostBuilderContext context, IConfigurationBuilder config) => config
            .AddFrameworkConfig(context);

        public static void ConfigureServices(HostBuilderContext context, IServiceCollection services) => services
            .AddPooledDbContextFactory<MigrationContext>(c => c
                .UseNpgsql(context.Configuration.GetConnectionString("PgsqlConnection"), b => b.EnableRetryOnFailure()));
    }
}
