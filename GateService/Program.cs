using GateService.Game;
using GateService.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ow.Framework.Game;
using ow.Framework.IO.Lan.Extensions;
using ow.Framework.IO.Network.Extensions;

namespace GateService
{
    public static class Program
    {
        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        public static IHostBuilder CreateHostBuilder(string[] args) => Host
            .CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) => services
                .AddHostedService<Worker>()
                .AddTransient<BinTable>()
                .AddSingleton<DataBinTable>()
                .AddSingleton<Districts>()
                .AddSingleton<Server>()
                .AddSingleton<Gate>()
                .AddSingleton<DataBinTable>()
                .AddTransient<Session>()
                .AddNetwork()
                .AddLan());
    }
}