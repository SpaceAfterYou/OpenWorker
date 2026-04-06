using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenWorker.Channel;
using OpenWorker.DependencyInjection;
using OpenWorker.Domain.Enums;
using OpenWorker.GateServer.Gameplay.Extensions;
using OpenWorker.GateServer.Server.Services;
using OpenWorker.Hotspot;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Res.Extensions;

await Host
    .CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddCache();
        services.AddSessionCache();
        services.AddGateCache();
        services.AddChannelCache();
        services.AddDistrictCache();
        services.AddDistrictReserveCache();

        services.AddRes();
        services.AddBatch();
        
        services.AddSingleton<WorldManager>();
        services.AddSingleton<StorageFactory>();
        services.AddPersistence();
        services.AddGameplay();
        
        services.AddHostedService<KeepAliveService>();

        services.AddHotspot(context, EntityComponentService.Gate);
    })
    .Build()
    .RunAsync()
    .ConfigureAwait(false);