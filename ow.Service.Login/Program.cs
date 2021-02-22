using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ow.Framework.Database.Extensions;
using ow.Framework.Extensions;
using ow.Framework.IO.Network.Relay.Extensions;
using ow.Service.Login.Network;
using SoulCore.IO.Network.Extensions;
using System.Threading.Tasks;

namespace ow.Service.Login
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
            .AddNetwork()
            .AddHostedService<SyncServer>()
            .AddAccountContext(context)
            .AddCharacterContext(context)
            .AddItemContext(context)
            .AddRelayHandlers();
    }
}