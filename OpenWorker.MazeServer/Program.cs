using OpenWorker.Batch;
using OpenWorker.Batch.Extensions;
using OpenWorker.Channel;
using OpenWorker.Channel.Extensions;
using OpenWorker.DependencyInjection;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Commands.Extensions;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Modules.Items;
using OpenWorker.MazeServer.Services;
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
        
        services.AddSingleton<MazeResourceProvider>();
        services.AddSingleton<DistrictResourceProvider>();
        services.AddSingleton<BatchManager>();
        services.AddSingleton<BuffManager>();
        
        services.AddStuffCommands();
        
        services.AddSingleton<WorldManager>();
        services.AddSingleton<PersonRegistry>();

        services.AddSingleton<StorageItemFactory>();
        services.AddSingleton<StorageFactory>();
        services.AddSingleton<StorageManager>();
        services.AddPersistence();
        
        services.AddChannels();

        // services.AddSingleton<ItemSystem>();
        // services.AddSingleton<NpcManager>();
        // services.AddSingleton<PersonRegistry>();

        services.AddHotspot(context, EntityComponentService.District);

        // services.AddHostedService<ServerBootstrapService>();
        // services.AddHostedService<KeepAliveService>();
    })
    .Build()
    .RunAsync()
    .ConfigureAwait(false);