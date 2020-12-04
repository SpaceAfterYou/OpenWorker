using Core.Systems.NetSystem.Providers;
using GateService.Systems.GameSystem;
using GateService.Systems.NetSystem;
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
                .AddSingleton<Tables>()
                .AddTransient<Session>());
    }
}