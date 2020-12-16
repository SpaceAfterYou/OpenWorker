using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ow.Framework.Game;
using ow.Framework.IO.Lan.Extensions;
using ow.Framework.IO.Network.Extensions;
using ow.Service.District.Game;
using ow.Service.District.Game.Repositories;
using ow.Service.District.Network.Repositories;

namespace ow.Service.District
{
    public static class Program
    {
        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        public static IHostBuilder CreateHostBuilder(string[] args) => Host
            .CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) => services
                .AddHostedService<Worker>()
                .AddSingleton<ChatCommandRepository>()
                .AddSingleton<DimensionRepository>()
                .AddSingleton<BinTables>()
                .AddSingleton<CachedNpcRepository>()
                .AddTransient<WorldTables>()
                .AddFramework()
                .AddLan());
    }
}