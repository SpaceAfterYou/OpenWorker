using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ow.Framework.Extensions;
using ow.Framework.Game;
using ow.Framework.IO.Network.Relay;
using ow.Framework.IO.Network.Relay.Extensions;
using ow.Framework.IO.Network.Sync.Extensions;
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
                .AddSingleton<Features>()
                .AddTransient<SyncSession>()
                .AddTransient<GlobalRelayClient>()
                .AddSingleton<SyncServer>()
                .AddAccountContext(context)
                .AddCharacterContext(context)
                .AddRelayHandlers());
    }
}