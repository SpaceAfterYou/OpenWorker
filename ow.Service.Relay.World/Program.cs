using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ow.Framework.Extensions;
using ow.Framework.IO.Network.Relay.Extensions;
using ow.Service.Relay.World.Network;
using ow.Service.Relay.World.Repository;
using System.Threading.Tasks;

namespace ow.Service.Relay.World
{
    public static class Program
    {
        public static Task Main(string[] args) => CreateHostBuilder(args).Build().RunAsync();

        public static IHostBuilder CreateHostBuilder(string[] args) => Host
            .CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(ConfigureAppConfiguration)
            .ConfigureServices(ConfigureServices);

        private static void ConfigureAppConfiguration(HostBuilderContext context, IConfigurationBuilder config) => config
            .AddFrameworkConfig(context);

        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services) => services
            .AddHostedService<Worker>()
            .AddTransient<RWServer>()
            .AddTransient<PartyRepository>()
            .AddRelayHandlers();
    }
}