using Core.Systems.NetSystem.Providers;
using LoginService.Systems.NetSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace LoginService
{
    public class Program
    {
        public static Task Main(string[] args) => CreateHostBuilder(args).Build().RunAsync();

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                    services
                        .AddHostedService<Worker>()
                        .AddSingleton<HandlerProvider>()
                        .AddSingleton<Server>()
                        .AddTransient<Session>()
                );
    }
}