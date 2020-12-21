using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ow.Framework.Extensions;
using ow.Framework.Game;
using ow.Framework.IO.Lan.Extensions;
using ow.Framework.IO.Network.Extensions;
using ow.Service.Login.Game.Repositories;
using ow.Service.Login.Network;
using System;

namespace ow.Service.Login
{
    public static class Program
    {
        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        public static IHostBuilder CreateHostBuilder(string[] args) => Host
            .CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddFramework();
            })
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