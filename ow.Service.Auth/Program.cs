using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ow.Framework.Extensions;
using ow.Framework.Game;
using ow.Framework.IO.Lan.Extensions;
using ow.Framework.IO.Network.Sync.Extensions;
using ow.Service.Auth.Game.Repositories;
using ow.Service.Auth.Network;

namespace ow.Service.Auth
{
    public static class Program
    {
        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        public static IHostBuilder CreateHostBuilder(string[] args) => Host
            .CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) => config
                .AddFramework(hostingContext))
            .ConfigureServices((hostContext, services) => services
                .AddHostedService<Worker>()
                .AddSingleton<GateRepository>()
                .AddSingleton<Server>()
                .AddSingleton<Features>()
                .AddTransient<Session>()
                .AddTransient<Server>()
                .AddFramework()
                .AddLan());
    }
}