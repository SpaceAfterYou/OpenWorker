using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ow.Framework.Extensions;
using ow.Framework.IO.Network.Relay.Extensions;
using ow.Framework.IO.Network.Relay.Global;
using ow.Service.Relay.Session.Repository;
using System.Threading.Tasks;

namespace ow.Service.Relay.Session
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
            .AddTransient<RGServer>()
            .AddTransient<SessionRepository>()
            .AddRelayHandlers();
    }
}