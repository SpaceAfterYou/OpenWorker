using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ow.Framework.Database.Extensions;
using ow.Framework.Extensions;
using ow.Framework.IO.File.World;
using ow.Framework.IO.Network.Relay.Extensions;
using ow.Framework.IO.Network.Relay.Global;
using ow.Service.District.Game;
using ow.Service.District.Game.Repositories;
using ow.Service.District.Network.Relay;
using ow.Service.District.Network.Sync;
using SoulCore.IO.Network.Extensions;

namespace ow.Service.District
{
    public static class Program
    {
        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        public static IHostBuilder CreateHostBuilder(string[] args) => Host
            .CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) => config
                .AddFrameworkConfig(context))
            .ConfigureServices((context, services) => services
                .AddNetwork()
                .AddHostedService<Worker>()
                .AddSingleton<ChatCommandRepository>()
                .AddSingleton<ChannelRepository>()
                .AddSingleton<DayEventBoosterRepository>()
                .AddSingleton<BoosterRepository>()
                .AddSingleton<NpcRepository>()
                .AddSingleton<GateInstance>()
                .AddTransient<WorldTable>()
                .AddSingleton<RWClient>()
                .AddSingleton<RGClient>()
                .AddSingleton<RWServer>()
                .AddAccountContext(context)
                .AddCharacterContext(context)
                .AddItemContext(context)
                .AddRelayHandlers());
    }
}