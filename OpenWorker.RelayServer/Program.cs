using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenWorker.DependencyInjection;
using OpenWorker.DependencyInjection.Health;
using OpenWorker.Extensions;
using OpenWorker.Hotspot.Cache;
using OpenWorker.Persistence;
using OpenWorker.RelayServer.DependencyInjection;
using OpenWorker.RelayServer.Services;
using OpenWorker.Res.Extensions;

await Host
    .CreateDefaultBuilder(args)
    .UseHealthEndpoint()
    .ConfigureServices((context, services) =>
    {
        services.AddCache();
        services.AddPersistence();
        services.AddTelemetry();
        services.AddCredentialsOptions();
        services.AddDiscord();
        services.AddRes();

        services.AddCacheBootstrapService();
        
        services.AddHostedService<DiscordService>();
    })
    .Build()
    .RunAsync()
    .ConfigureAwait(false);