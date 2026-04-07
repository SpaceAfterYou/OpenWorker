using OpenWorker.Batch;
using OpenWorker.Channel;
using OpenWorker.Channel.Extensions;
using OpenWorker.Commands;
using OpenWorker.Commands.Extensions;
using OpenWorker.DependencyInjection;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Gameplay;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Quests;
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

        services.AddCommandManager();

        services.AddSingleton<WorldManager>();
        services.AddSingleton<PersonRegistry>();

        services.AddSingleton<StorageItemFactory>();
        services.AddSingleton<StorageFactory>();
        services.AddSingleton<StorageManager>();
        services.AddSingleton<QuestManager>();
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
