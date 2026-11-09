using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenWorker.Channel.Extensions;
using OpenWorker.DependencyInjection;
using OpenWorker.DependencyInjection.Health;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.GateServer.Gameplay.Extensions;
using OpenWorker.GateServer.Server.Services;
using OpenWorker.Hotspot;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Res.Extensions;

await Host
    .CreateDefaultBuilder(args)
    .UseHealthEndpoint()
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
        
        services.AddPlayerTransfer();
        services.AddSingleton<ItemCatalog>();
        services.AddSingleton<DatabaseItemSerials>();
        services.AddSingleton<IItemSerials>(provider => provider.GetRequiredService<DatabaseItemSerials>());
        services.AddSingleton<InventoryFactory>();
        services.AddSingleton<InventoryPersistence>();
        services.AddPersistence();
        services.AddGameplay();
        
        services.AddGateOptions();
        services.AddGateEndpointOptions();
        services.AddPersonCreationOptions();
        services.AddRegistrationService<GateRegistrationService>();

        services.AddHotspot(context, EntityComponentService.Gate);
    })
    .Build()
    .RunAsync();