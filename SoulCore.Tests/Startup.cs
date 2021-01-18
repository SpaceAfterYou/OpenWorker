using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SoulCore.IO.File.Bin;
using System.IO;

namespace SoulCore.Tests
{
    public class Startup
    {
        public ServiceProvider ServiceProvider { get; }

        public Startup() => ServiceProvider = new ServiceCollection()
            .AddSingleton(GetConfiguration())
            .AddSingleton<BinTable>()
            .BuildServiceProvider();

        private static IConfiguration GetConfiguration() => new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("config/appsettings.json", false, true)
            .Build();
    }
}
