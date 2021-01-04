using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ow.Framework.Extensions;
using ow.Framework.IO.Network.Relay.Extensions;
using ow.Framework.IO.Network.Sync.Extensions;
using ow.Service.Gate.Game;
using ow.Service.Gate.Game.Repository;
using ow.Service.Gate.Network;
using ow.Service.Gate.Network.Relay;

namespace ow.Service.Gate
{
    public static class Program
    {
        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        public static IHostBuilder CreateHostBuilder(string[] args) => Host
            .CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) => config
                .AddFrameworkConfig(context))
            .ConfigureServices((context, services) => services
                .AddSyncHandlers()
                .AddHostedService<Worker>()
                .AddSingleton<DistrictRepository>()
                .AddSingleton<GateInstance>()
                .AddSingleton<RelayClient>()
                .AddSingleton<BinTables>()
                .AddTransient<Session>()
                .AddSingleton<Server>()
                .AddAccountContext(context)
                .AddCharacterContext(context)
                .AddItemContext(context)
                .AddRelayChannel()
                .AddRelayHandlers());
    }
}