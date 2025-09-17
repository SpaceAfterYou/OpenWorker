using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenWorker.DependencyInjection;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Persistence;
using OpenWorker.RelayServer.DependencyInjection;
using OpenWorker.RelayServer.Services;
using OpenWorker.Res.Extensions;

await Host
    .CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddCache();
        services.AddPersistence();
        services.AddDiscord();
        services.AddRes();

        services.AddCacheBootstrapService();
        services.AddPersistenceBootstrapService();
        
        services.AddHostedService<DiscordService>();
    })
    .Build()
    .RunAsync()
    .ConfigureAwait(false);