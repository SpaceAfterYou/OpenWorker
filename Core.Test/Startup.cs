using Core.Systems.GameSystem;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace Core.Test
{
    public class Startup
    {
        public ServiceProvider ServiceProvider { get; }

        public Startup()
        {
            ServiceCollection serviceCollection = new();

            serviceCollection.AddSingleton(GetConfiguration());
            serviceCollection.AddSingleton<BinTableProcessor>();

            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        private static IConfiguration GetConfiguration() => new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(
                    path: "appsettings.json",
                    optional: false,
                    reloadOnChange: true)
            .Build();
    }
}