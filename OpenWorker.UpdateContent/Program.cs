using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenWorker.Extensions;
using OpenWorker.Persistence;

// The settings of a server: appsettings.json next to the assembly, then the environment and the command line.
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

await using var provider = new ServiceCollection()
    .AddSingleton<IConfiguration>(configuration)
    .AddPersistenceOptions()
    .BuildServiceProvider();

// A missing connection string stops here and names the setting.
var options = provider.GetRequiredService<IOptions<PersistenceOptions>>().Value;

var builder = new DbContextOptionsBuilder<PersistenceContext>()
    .UseNpgsql(options.ConnectionString);

await using var context = new PersistenceContext(builder.Options);
await context.Database.MigrateAsync();
await context.SaveChangesAsync();
