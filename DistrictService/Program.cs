using Core.Systems.GameSystem;
using Core.Systems.LanSystem.Extensions;
using DistrictService.Systems.GameSystem;
using DistrictService.Systems.NetSystem;
using DistrictService.Systems.NetSystem.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DistrictService
{
    public static class Program
    {
        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        public static IHostBuilder CreateHostBuilder(string[] args) => Host
            .CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) => services
                .AddHostedService<Worker>()
                .AddSingleton<Server>()
                .AddSingleton<ChatCommandRepository>()
                .AddSingleton<BinTable>()
                .AddTransient<BinTableProcessor>()
                .AddTransient<WorldTableProcessor>()
                .AddRedis()
                .AddTransient<Session>());
    }
}