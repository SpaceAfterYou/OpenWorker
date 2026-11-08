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
using OpenWorker.Gameplay.Ai;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Extensions;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Lua;
using OpenWorker.Lua.Ai;
using OpenWorker.MazeServer.Services;
using OpenWorker.Res.Extensions;

await Host
    .CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        context.Configuration[ConfigurationUtils.InstanceIdentifierKey] = Guid.CreateVersion7().ToString();

        services.AddCache();
        services.AddSessionCache();
        services.AddGateCache();
        services.AddChannelCache();
        services.AddDistrictCache();
        services.AddDistrictReserveCache();

        services.AddRes();
        services.AddBatch();

        services.AddSingleton<MazeResourceProvider>();
        services.AddSingleton<DistrictResourceProvider>();
        services.AddSingleton<BatchManager>();
        services.AddSingleton<BuffManager>();
        services.AddSingleton<CombatTables>();
        services.AddSingleton(provider => new AiScriptLoader(provider.GetRequiredService<ILogger<AiScriptLoader>>()));
        services.AddSingleton<MonsterBehaviorTables>();
        services.AddSingleton<CreatureServices>();
        services.AddSingleton<SimulationClock>();
        services.AddHostedService(provider => provider.GetRequiredService<SimulationClock>());

        services.AddCommandManager();

        services.AddSingleton<WorldManager>();
        services.AddSingleton<LeaveManager>();
        services.AddSingleton<PersonRegistry>();
        services.AddSingleton<ISessionCloseHandler, PlayerStorageCleaner>();
        services.AddSingleton<ISessionCloseHandler, MazeCloseHandler>();

        services.AddSingleton<StorageItemFactory>();
        services.AddSingleton<StorageFactory>();
        services.AddSingleton<CostumeFactory>();
        services.AddSingleton<StorageManager>();
        services.AddSingleton<QuestManager>();
        services.AddPersistence();
        services.AddMiscGameplay();

        services.AddChannels();

        // services.AddSingleton<ItemSystem>();
        // services.AddSingleton<NpcManager>();
        // services.AddSingleton<PersonRegistry>();

        services.AddHotspot(context, EntityComponentService.District);

        // services.AddHostedService<ServerBootstrapService>();
        // services.AddHostedService<KeepAliveService>();
    })
    .Build()
    .RunAsync();
