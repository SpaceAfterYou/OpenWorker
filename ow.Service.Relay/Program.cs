using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ow.Framework.Extensions;
using ow.Framework.IO.Network.Relay.Extensions;
using ow.Service.Relay.Repository;
using ow.Service.World.Network.Relay;

namespace ow.Service.Relay
{
    public class Program
    {
        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        public static IHostBuilder CreateHostBuilder(string[] args) => Host
            .CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) => config
                .AddFrameworkConfig(context))
            .ConfigureServices((hostContext, services) => services
                .AddHostedService<Worker>()
                .AddTransient<GlobalRelayServer>()
                .AddTransient<SessionRepository>()
                .AddRelayHandlers());
    }
}