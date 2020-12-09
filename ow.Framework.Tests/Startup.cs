using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ow.Framework.Game;
using System.IO;

namespace ow.Framework.Tests
{
    public class Startup
    {
        public ServiceProvider ServiceProvider { get; }

        public Startup() => ServiceProvider = new ServiceCollection()
            .AddSingleton(GetConfiguration())
            .AddSingleton<BinTableProcessor>()
            .BuildServiceProvider();

        private static IConfiguration GetConfiguration() => new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false, true)
            .Build();
    }
}