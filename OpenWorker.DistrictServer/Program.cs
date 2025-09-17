using OpenWorker.Batch.Extensions;
using OpenWorker.Channel;
using OpenWorker.Channel.Extensions;
using OpenWorker.DependencyInjection;
using OpenWorker.DistrictServer.Gameplay.Extensions;
using OpenWorker.DistrictServer.Server;
using OpenWorker.DistrictServer.Server.Services;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Commands.Extensions;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Modules.Items;
using OpenWorker.Res.Extensions;

await Host
    .CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        context.Configuration[ConfigurationUtils.InstanceIdentifierKey] = Guid.CreateVersion7().ToString();

        services.AddCache();
        services.AddSessionCache();
        services.AddChannelCache();
        services.AddDistrictCache();
        services.AddDistrictReserveCache();
        services.AddRes();
        services.AddBatch();
        
        services.AddSingleton<WorldManager>();

        services.AddSingleton<StorageItemFactory>();
        services.AddSingleton<StorageFactory>();
        services.AddSingleton<StorageManager>();
        services.AddPersistence();
        services.AddGameplay();
        
        services.AddStuffCommands();
        services.AddChannels();

        services.AddSingleton<NpcManager>();
        services.AddSingleton<PersonRegistry>();

        services.AddHotspot(context, EntityComponentService.District);

        services.AddHostedService<ServerBootstrapService>();
        services.AddHostedService<KeepAliveService>();
    })
    .Build()
    .RunAsync()
    .ConfigureAwait(false);