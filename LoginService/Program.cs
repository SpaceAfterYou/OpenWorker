using Core.Systems.NetSystem.Providers;
using LoginService.Systems.GameSystem;
using LoginService.Systems.NetSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LoginService
{
    public class Program
    {
        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        public static IHostBuilder CreateHostBuilder(string[] args) => Host
            .CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) => services
                .AddHostedService<Worker>()
                .AddSingleton<HandlerProvider>()
                .AddSingleton<Server>()
                .AddSingleton<Gates>()
                .AddSingleton<Options>()
                .AddTransient<Session>());
    }
}