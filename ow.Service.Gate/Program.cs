using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ow.Framework.Game;
using ow.Framework.Game.Datas.Bin.Table;
using ow.Framework.IO.Lan.Extensions;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Extensions;
using ow.Service.Gate.Game;

namespace ow.Service.Gate
{
    public static class Program
    {
        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        public static IHostBuilder CreateHostBuilder(string[] args) => Host
            .CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) => services
                .AddHostedService<Worker>()
                .AddTransient<BinTable>()
                .AddSingleton<IBinTables, BinTables>()
                .AddSingleton<DistrictsInstances>()
                .AddSingleton<GameServer>()
                .AddSingleton<GateInfo>()
                .AddTransient<GameSession>()
                .AddNetwork()
                .AddLan());
    }
}