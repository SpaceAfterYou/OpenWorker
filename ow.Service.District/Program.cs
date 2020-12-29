using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ow.Framework.Extensions;
using ow.Framework.IO.File.World;
using ow.Framework.IO.Lan.Extensions;
using ow.Framework.IO.Network.Sync.Extensions;
using ow.Service.District.Game;
using ow.Service.District.Game.Repositories;
using ow.Service.District.Network;
using ow.Service.District.Network.Sync;
using ow.Service.District.Network.Sync.Repositories;

namespace ow.Service.District
{
    internal static class Program
    {
        internal static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        internal static IHostBuilder CreateHostBuilder(string[] args) => Host
            .CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) => config
                .AddFramework(hostingContext))
            .ConfigureServices((hostContext, services) => services
                .AddHostedService<Worker>()
                .AddSingleton<ChatCommandRepository>()
                .AddSingleton<DimensionRepository>()
                .AddSingleton<BinTables>()
                .AddSingleton<Instance>()
                .AddSingleton<DayEventBoosterRepository>()
                .AddSingleton<BoosterRepository>()
                .AddSingleton<NpcRepository>()
                .AddSingleton<Server>()
                .AddSingleton<GateInstance>()
                .AddTransient<WorldTable>()
                .AddTransient<Session>()
                .AddFramework()
                .AddLan());
    }
}