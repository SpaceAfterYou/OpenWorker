using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SoulCore.Extensions;
using SoulCore.Game;
using SoulCore.IO.Network.Relay.Extensions;
using SoulCore.IO.Network.Relay.Global;
using SoulCore.IO.Network.Sync.Extensions;
using ow.Service.Auth.Game.Repositories;
using ow.Service.Auth.Network.Sync;

namespace ow.Service.Auth
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
                .AddSingleton<GateRepository>()
                .AddSingleton<Options>()
                .AddTransient<SyncSession>()
                .AddTransient<RGClient>()
                .AddSingleton<SyncServer>()
                .AddAccountContext(context)
                .AddCharacterContext(context)
                .AddRelayHandlers());
    }
}
