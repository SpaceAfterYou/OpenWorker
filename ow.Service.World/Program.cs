using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ow.Framework.Extensions;
using ow.Framework.IO.Network.Relay.Extensions;
using ow.Framework.IO.Network.Relay.Global;
using ow.Framework.IO.Network.Sync.Extensions;
using ow.Service.World.Game;
using ow.Service.World.Game.Gate;
using ow.Service.World.Game.Gate.Repository;
using ow.Service.World.Network.Gate.Sync;
using ow.Service.World.Network.Relay;

namespace ow.Service.World
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
                .AddSingleton<RGClient>()
                .AddSingleton<PartyRepository>()
                .AddSingleton<Instance>()
                .AddSingleton<BinTables>()
                .AddTransient<RWServer>()
                .AddTransient<SyncSession>()
                .AddSingleton<SyncServer>()
                .AddAccountContext(context)
                .AddCharacterContext(context)
                .AddItemContext(context)
                .AddRelayHandlers());
    }
}