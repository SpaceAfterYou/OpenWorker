using Core.Systems.GameSystem;
using Core.Systems.LanSystem;
using Core.Systems.LanSystem.Extensions;
using Core.Systems.NetSystem.Providers;
using GateService.Systems.GameSystem;
using GateService.Systems.NetSystem;
using GateService.Systems.ServiceSystem.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GateService
{
    public static class Program
    {
        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        public static IHostBuilder CreateHostBuilder(string[] args) => Host
            .CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) => services
                .AddHostedService<Worker>()
                .AddSingleton<HandlerProvider>()
                .AddSingleton<Districts>()
                .AddSingleton<Server>()
                .AddSingleton<Gate>()
                .AddSingleton<BinTable>()
                .AddTransient<LanContext>()
                .AddTransient<Runner>()
                .AddTransient<BinTableProcessor>()
                .AddTransient<Session>()
                .AddRedis()
                .AddTables(hostContext.Configuration));
    }
}